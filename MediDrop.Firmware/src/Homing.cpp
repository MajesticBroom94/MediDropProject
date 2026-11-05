#include <Homing.h>
#include <Types.h>
#include <Arduino.h>
#include <../include/Hardware.h>

static unsigned long homingStepStartTime;
static HomingStatus homing_status = HOMING_START;

void homingSequenceExecute()
{
    switch (homing_status) {

    case HOMING_START:
        {
            if (switchTriggered(LIMIT_SWITCH))
            {
                stepper.move(-280);
                homingStepStartTime = millis();
                homing_status = BACKOFF_FROM_LIMIT;
            }
            else
            {
                stepper.setSpeed(600);
                homing_status = RETURNING_HOME;
            }
        }
        break;

    case RETURNING_HOME:
        {
            if (!switchTriggered(LIMIT_SWITCH))
                stepper.runSpeed();
            else
            {
                stepper.setSpeed(0);
                stepper.move(-280);
                homingStepStartTime = millis();
                homing_status = BACKOFF_FROM_LIMIT;
            }
        }
        break;

    case BACKOFF_FROM_LIMIT:
        {
            if (millis() - homingStepStartTime < 350)
                break;

            if (stepper.distanceToGo() != 0)
                stepper.run();

            else
            {
                homingStepStartTime = millis();
                stepper.setSpeed(400);
                homing_status = SET_FINAL_POSITION;
            }
        }
        break;

    case SET_FINAL_POSITION:
        {
            if (millis() - homingStepStartTime < 350)
                break;

            if (!switchTriggered(LIMIT_SWITCH))
                stepper.runSpeed();

            else
            {
                stepper.setSpeed(0);
                stepper.setCurrentPosition(0);
                homing_status = HOME_POSITION_VALID;
            }
        }
        break;

    case HOME_POSITION_VALID:
    case HOMING_REQUIRED:
        break;
    }
}

bool isHomed()
{
    return homing_status == HOME_POSITION_VALID;
}

void unsetHomedPosition()
{
    homing_status = HOMING_REQUIRED;
}

void startHoming()
{
    if (homing_status == HOMING_REQUIRED)
    {
        homing_status = HOMING_START;
    }
}
