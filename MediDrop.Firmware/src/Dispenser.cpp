#include "Dispenser.h"
#include "Types.h"
#include "Hardware.h"
#include "Homing.h"
#include "DeviceEvent.h"
#include "DeviceApi.h"
#include "LocalSchedule.h"
#include "Purge.h"
#include "LCD.h"
#include <atomic>
#include <cstring>

static std::atomic<Status> status{INITIALIZING};
static char operationId[37] = {};
static uint8_t slot = 0;
static int slotGroup = 0;
static int currentServoPin = 0;
static long dispensePosition = 0;

static unsigned long rightButtonPressStart = 0;
static unsigned long stateStartTime = 0;
static unsigned long lastScaleCheck = 0;
static constexpr unsigned long DISPENSER_WAIT_TIME = 800;
static constexpr unsigned long SCALE_WAIT_TIME = 2000;
static constexpr unsigned long CANCEL_HOLD_TIME = 2300;
static constexpr unsigned long CONFIRM_TIMEOUT = 60UL * 60UL * 1000UL;
static constexpr unsigned long COLLECTION_TIMEOUT = 20UL * 60UL * 1000UL;
static constexpr unsigned long SCALE_CHECK_INTERVAL = 100;
static constexpr float REMOVAL_THRESHOLD = 0.15f; // grams; set from presentation readings

static float initialWeight = 0;
static float collectedWeight = 0;
static float removalSamples[3] = {};
static uint8_t readingCount = 0;
static uint8_t lowWeightCount = 0;

static void completeOperation() {
    if (!operationId[0]) return;
    scheduleFinish(String(operationId));
    operationId[0] = '\0';
    slot = 0;
}

static void returnHome() {
    startHoming();
    status = WAIT_FOR_HOMING;
}

bool assignTask(const char* newOperationId, uint8_t newSlot) {
    if (dispenserBusy() || !isHomed() || newSlot < 1 || newSlot > 21 ||
        !newOperationId || strlen(newOperationId) != 36) return false;

    strlcpy(operationId, newOperationId, sizeof(operationId));
    slot = newSlot;
    clearRemoteAuthorization();

    if (scaleIsReady())
    {
        scaleTare();
    }

    collectedWeight = 0;
    readingCount = 0;
    status = ASSIGNED_TASK;
    return true;
}

void dispenserSequenceExecute() {
    switch (status.load()) {
    case INITIALIZING:
        if (isHomed()) status = IDLE;
        break;

    case IDLE:
        if (lcdEmptyAlertActive())
        {
            if (switchTriggered(LEFT_BTN) || switchTriggered(RIGHT_BTN))
            {
                lcdDismissEmptyAlert();
            }
        }
        break;

    case ASSIGNED_TASK:
        unsetHomedPosition();
        slotGroup = (slot - 1) / 3;
        currentServoPin = (slot + 2) % 3;
        dispensePosition = slotGroup * STEPPER_INCREMENT * -1;
        stepper.moveTo(dispensePosition);
        status = MOVE_TO_CONTAINER_POSITION;
        break;

    case MOVE_TO_CONTAINER_POSITION:
        if (stepper.distanceToGo() != 0) {
            stepper.run();
        } else {
            stateStartTime = millis();
            status = WAIT_CONFIRM;
        }
        break;

    case WAIT_CONFIRM:
        if (switchTriggered(LEFT_BTN) || takeRemoteAuthorization()) {
            lcdNotify(LcdNotice::DispenseConfirmed, slot);
            status = SERVO_UP;
            break;
        }
        if (switchTriggered(RIGHT_BTN)) {
            rightButtonPressStart = millis();
            status = WAIT_CANCEL_HOLD;
            break;
        }
        if (millis() - stateStartTime >= CONFIRM_TIMEOUT) {
            queueDeviceEvent(DeviceEventType::DispenseTimedOut, operationId);
            lcdNotify(LcdNotice::DispenseTimedOut, slot);
            completeOperation();
            returnHome();
        }
        break;

    case WAIT_CANCEL_HOLD:
        if (!switchTriggered(RIGHT_BTN)) {
            status = WAIT_CONFIRM; // don't restart the 60-minute timeout
            break;
        }
        if (millis() - rightButtonPressStart >= CANCEL_HOLD_TIME) {
            queueDeviceEvent(DeviceEventType::DispenseCancelled, operationId);
            lcdNotify(LcdNotice::DispenseCancelled, slot);
            completeOperation();
            returnHome();
        }
        break;

    case SERVO_UP: {
        float progress = servo.write(servoPins[currentServoPin], 78, SERVO_SPEED, SERVO_EASING);
        if (progress >= 1.0f) {
            stateStartTime = millis();
            status = SERVO_DOWN;
        }
        break;
    }

    case SERVO_DOWN:
        if (millis() - stateStartTime >= DISPENSER_WAIT_TIME) {
            float progress = servo.write(servoPins[currentServoPin], 0, SERVO_SPEED, SERVO_EASING);
            if (progress <= 0.0f) {
                stepper.moveTo(-4350);
                status = MOVE_TO_RELEASE_POSITION;
            }
        }
        break;

    case MOVE_TO_RELEASE_POSITION:
        if (stepper.distanceToGo() != 0) {
            stepper.run();
        } else {
            // Dispensed != collected. Keep the operation ID until collection outcome.
            queueDeviceEvent(DeviceEventType::DispenseCompleted, operationId);
            stateStartTime = millis();
            readingCount = 0;
            status = WAIT_FOR_SCALE_READING;
        }
        break;

    case WAIT_FOR_SCALE_READING:
        // Allow pill to settle for two seconds, then average five ready HX711 samples.
        if (millis() - stateStartTime < SCALE_WAIT_TIME)
            break;

        if (!scaleIsReady())
            break;

        collectedWeight += scaleGetWeight();
        ++readingCount;

        if (readingCount >= 5) {
            initialWeight = collectedWeight / 5.0f;
            collectedWeight = 0;
            readingCount = 0;
            lowWeightCount = 0;
            lastScaleCheck = millis();
            stateStartTime = millis();
            Serial.printf("[SCALE] Medication baseline: %.3f g\n", initialWeight);
            status = WAIT_FOR_MEDICATION_REMOVAL;
        }
        break;

    case WAIT_FOR_MEDICATION_REMOVAL: {
        if (millis() - stateStartTime >= COLLECTION_TIMEOUT) {
            queueDeviceEvent(DeviceEventType::MedicationCollectionTimedOut, operationId);
            lcdNotify(LcdNotice::CollectionTimedOut, slot);
            completeOperation();
            returnHome();
            break;
        }
        if (millis() - lastScaleCheck < SCALE_CHECK_INTERVAL || !scaleIsReady())
        {
            break;
        }

        lastScaleCheck = millis();
        float grams = scaleGetWeight();

        removalSamples[readingCount++] = grams;
        if (readingCount < 3)
            break;

        readingCount = 0;

        float averagedWeight = (removalSamples[0]+removalSamples[1]+removalSamples[2]) / 3.0f;

        if (initialWeight - averagedWeight >= REMOVAL_THRESHOLD)
            ++lowWeightCount;

        else lowWeightCount = 0;

        if (lowWeightCount >= 3) {
            queueDeviceEvent(DeviceEventType::MedicationCollected, operationId);
            lcdNotify(LcdNotice::MedicationCollected, slot);
            completeOperation();
            returnHome();
        }
        break;
    }

    case WAIT_FOR_HOMING:
        if (isHomed()) status = IDLE;
        break;
    }
}

Status dispenserStatus()
{
    return status.load();
}
bool dispenserBusy()
{
    return status.load() != IDLE || purgeBusy();
}

uint8_t dispenserCurrentSlot() { return slot; }
