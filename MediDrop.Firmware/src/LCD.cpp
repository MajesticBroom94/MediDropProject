#include "LCD.h"
#include "Hardware.h"
#include "Types.h"
#include "Dispenser.h"
#include "Purge.h"
#include "Buzzer.h"
#include "DeviceClock.h"
#include "LocalSchedule.h"
#include "DeviceApi.h"
#include "Homing.h"
#include <WiFi.h>
#include <time.h>

// LCD is 16 columns x 2 rows. Only redraw changed lines: no flickering
// lcd.clear() on every loop iteration.
static String previousTop;
static String previousBottom;
static bool noticeActive = false;
static unsigned long noticeStartedAt = 0;
static constexpr unsigned long NOTICE_DURATION_MS = 2500;
static unsigned long lastRefreshAt = 0;
static constexpr unsigned long REFRESH_MS = 300;
static String noticeTop;
static String noticeBottom;
static bool emptyAlertArmed = false;
static unsigned long rightHoldStartedAt = 0;
static unsigned long pairingShownAt = 0;
static bool pairingTriggered = false;
static bool pairingNoticeActive = false;
static String pairingNoticeBottom;
static constexpr unsigned long PAIRING_HOLD_MS = 4000;
static constexpr unsigned long PAIRING_DISPLAY_MS = 8000;

static String fit16(String text)
{
    if (text.length() > 16) text.remove(16);
    while (text.length() < 16) text += ' ';
    return text;
}

static void show(const String& top, const String& bottom)
{
    String a = fit16(top);
    String b = fit16(bottom);
    if (a != previousTop) {
        lcd.setCursor(0, 0);
        lcd.print(a);
        previousTop = a;
    }
    if (b != previousBottom) {
        lcd.setCursor(0, 1);
        lcd.print(b);
        previousBottom = b;
    }
}

static String slotLabel(const char* prefix, uint8_t number)
{
    return String(prefix) + String(number);
}

static String localTime(int64_t timestamp)
{
    if (timestamp <= 0) return "--:--";
    // MediDrop's demo schedules use South African time (UTC+02:00).
    time_t seconds = static_cast<time_t>(timestamp + 2LL * 3600LL);
    struct tm parts{};
    gmtime_r(&seconds, &parts);
    char result[8];
    strftime(result, sizeof(result), "%H:%M", &parts);
    return String(result);
}

void lcdNotify(LcdNotice notice, uint8_t number)
{
    switch (notice) {
        case LcdNotice::DispenseConfirmed:
            noticeTop = "Dispense OK";
            noticeBottom = slotLabel("Slot ", number);
            break;
        case LcdNotice::DispenseCancelled:
            noticeTop = "Dispense cancel";
            noticeBottom = slotLabel("Slot ", number);
            break;
        case LcdNotice::DispenseTimedOut:
            noticeTop = "Confirm expired";
            noticeBottom = slotLabel("Missed slot ", number);
            break;
        case LcdNotice::MedicationCollected:
            noticeTop = "Medication taken";
            noticeBottom = slotLabel("Slot ", number) + " comp";
            break;
        case LcdNotice::CollectionTimedOut:
            noticeTop = "Not collected";
            noticeBottom = slotLabel("Slot ", number);
            break;
    }
    noticeActive = true;
    noticeStartedAt = millis();
}

void lcdNotifyAutoPurgeComplete()
{
    emptyAlertArmed = true;
    noticeTop = "Round complete";
    noticeBottom = "Dispenser empty";
    noticeActive = true;
    noticeStartedAt = millis();
}

void lcdPairingButtonExecute()
{
    // The RIGHT button remains available for cancel confirmation in other states.
    if (dispenserStatus() != IDLE || purgeBusy())
    {
        rightHoldStartedAt = 0;
        pairingTriggered = false;
        pairingNoticeActive = false;
        return;
    }

    if (!switchTriggered(RIGHT_BTN))
    {
        rightHoldStartedAt = 0;
        pairingTriggered = false;
        return;
    }

    if (rightHoldStartedAt == 0)
        rightHoldStartedAt = millis();

    if (!pairingTriggered && millis() - rightHoldStartedAt >= PAIRING_HOLD_MS)
    {
        pairingTriggered = true;  // Once per press, not every loop.
        pairingShownAt = millis();
        pairingNoticeActive = true;

        String code;
        if (devicePairingIsPaired())
            pairingNoticeBottom = "Already paired";
        else if (devicePairingCode(code))
            pairingNoticeBottom = code.substring(0, 3) + " " + code.substring(3);
        else
            pairingNoticeBottom = "Unavailable";
    }
}

void lcdExecute()
{
    // Existing pin assignments are correct: LED1 = blue, LED2 = red.
    // Blue is the physical Wi-Fi association indicator, not API availability.
    digitalWrite(LED1, WiFi.status() == WL_CONNECTED ? HIGH : LOW);

    const long long currentRound = scheduleRoundId();
    const bool noRound = currentRound == 0;
    digitalWrite(LED2, noRound ? HIGH : LOW);
    if (!noRound) emptyAlertArmed = false;

    const Status state = dispenserStatus();
    BuzzerReminder reminder = BuzzerReminder::None;
    if (state == WAIT_CONFIRM || state == WAIT_CANCEL_HOLD)
        reminder = BuzzerReminder::ConfirmDispense;
    else if (state == WAIT_FOR_MEDICATION_REMOVAL)
        reminder = BuzzerReminder::RemoveMedication;
    else if (emptyAlertArmed && noRound)
        reminder = BuzzerReminder::EmptyAfterPurge;
    buzzerSetReminder(reminder);

    if (millis() - lastRefreshAt < REFRESH_MS) return;
    lastRefreshAt = millis();

    if (noticeActive) {
        if (millis() - noticeStartedAt < NOTICE_DURATION_MS) {
            show(noticeTop, noticeBottom);
            return;
        }
        noticeActive = false;
    }

    if (pairingNoticeActive)
    {
        if (state == IDLE && !purgeBusy() &&
            millis() - pairingShownAt < PAIRING_DISPLAY_MS)
        {
            show("Pairing code", pairingNoticeBottom);
            return;
        }
        pairingNoticeActive = false;
    }

    if (purgeBusy()) {
        // Purging can be automatic or manually requested; the mechanics are shared.
        uint8_t group = purgeCurrentGroup();
        if (group == 0) show("Purge complete", "Returning home");
        else show("Clearing slots", "Group " + String(group) + " of 7");
        return;
    }

    const uint8_t currentSlot = dispenserCurrentSlot();
    switch (state) {
        case INITIALIZING:
            show("MediDrop", "Initializing...");
            return;
        case ASSIGNED_TASK:
        case MOVE_TO_CONTAINER_POSITION:
            show(slotLabel("Preparing S", currentSlot), "Please wait...");
            return;
        case WAIT_CONFIRM:
            show(slotLabel("Confirm slot ", currentSlot), "Left=OK Right=No");
            return;
        case WAIT_CANCEL_HOLD:
            show(slotLabel("Cancel slot ", currentSlot), "Keep holding...");
            return;
        case SERVO_UP:
        case SERVO_DOWN:
        case MOVE_TO_RELEASE_POSITION:
            show("Dispensing...", slotLabel("Slot ", currentSlot));
            return;
        case WAIT_FOR_SCALE_READING:
            show("Medication ready", "Checking tray...");
            return;
        case WAIT_FOR_MEDICATION_REMOVAL:
            show("Remove medication", "from tray");
            return;
        case WAIT_FOR_HOMING:
            show("Returning home", "Please wait...");
            return;
        case IDLE:
            break;
    }

    String clock = localTime(deviceClockNow());
    if (noRound) {
        show("MediDrop " + clock, "No active round");
        return;
    }

    ScheduledSlot next;
    if (scheduleNextPending(next)) {
        show("MediDrop " + clock,
             "Next S" + String(next.number) + " " + localTime(next.dueUtc));
    } else if (scheduleIsClosing()) {
        show("MediDrop " + clock, "Finishing round");
    } else {
        show("MediDrop " + clock, "Round complete");
    }
}


bool lcdEmptyAlertActive()
{
    return emptyAlertArmed;
}

void lcdDismissEmptyAlert()
{
    emptyAlertArmed = false;
    buzzerSetReminder(BuzzerReminder::None);
}