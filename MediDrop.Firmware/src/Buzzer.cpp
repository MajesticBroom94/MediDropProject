#include "Buzzer.h"
#include <Types.h>
#include <Hardware.h>

static constexpr unsigned long normalBeepTime = 700;
static constexpr unsigned long normalPauseTime = 500;
static constexpr unsigned long errorBeepTime = 250;
static constexpr unsigned long errorPauseTime = 150;
static constexpr unsigned long errorRepeatPauseTime = 600;
static constexpr unsigned long longBeepTime = 1200;

static constexpr unsigned long REMINDER_INTERVAL_MS = 60000UL;
static BuzzerReminder currentReminder = BuzzerReminder::None;
static unsigned long reminderStartedAt = 0;
static BuzzerState buzzer_state = BUZZER_OFF;
static unsigned long buzzerStateStartTime;
static std::uint8_t buzzerRingCounter = 0;

void buzzerToneExecute()
{
    switch (buzzer_state)
    {
    case BUZZER_OFF:
        break;

    case BUZZER_STOP:
        digitalWrite(BUZZER, LOW);
        buzzer_state = BUZZER_OFF;
        break;

        // ============================================
        // LONG BUZZER
        // ============================================
    case BUZZER_LONG_ON:
        buzzerStateStartTime = millis();
        digitalWrite(BUZZER, HIGH);
        buzzer_state = BUZZER_LONG_HOLD;
        break;

    case BUZZER_LONG_HOLD:
        if (millis() - buzzerStateStartTime >= longBeepTime)
            buzzer_state = BUZZER_STOP;
        break;

        // ============================================
        // NORMAL BUZZER
        // ============================================
    case BUZZER_NORMAL_ON:
        buzzerStateStartTime = millis();
        digitalWrite(BUZZER, HIGH);
        buzzerRingCounter++;
        buzzer_state = BUZZER_NORMAL_HOLD;
        break;

    case BUZZER_NORMAL_HOLD:
        {
            if (millis() - buzzerStateStartTime < normalBeepTime)
                break;

            if (buzzerRingCounter >= 3)
            {
                buzzer_state = BUZZER_STOP;
                break;
            }
            buzzerStateStartTime = millis();
            digitalWrite(BUZZER, LOW);
            buzzer_state = BUZZER_NORMAL_PAUSE;
        }
        break;


    case BUZZER_NORMAL_PAUSE:
        if (millis() - buzzerStateStartTime >= normalPauseTime)
            buzzer_state = BUZZER_NORMAL_ON;
        break;

    // ============================================
    // ERROR BUZZER
    // ============================================
    case BUZZER_ERROR_ON:
        buzzerStateStartTime = millis();
        digitalWrite(BUZZER, HIGH);
        buzzerRingCounter++;
        buzzer_state = BUZZER_ERROR_HOLD;
    break;

    case BUZZER_ERROR_HOLD:
        {
            if (millis() - buzzerStateStartTime < errorBeepTime)
                break;

            if (buzzerRingCounter >= 8)
            {
                buzzer_state = BUZZER_STOP;
                break;
            }

            buzzerStateStartTime = millis();
            digitalWrite(BUZZER, LOW);
            buzzer_state = BUZZER_ERROR_PAUSE;
        }
    break;

    case BUZZER_ERROR_PAUSE:
        if (buzzerRingCounter == 4)
        {
            if (millis() - buzzerStateStartTime < errorRepeatPauseTime)
                break;
        }
        else
        {
            if (millis() - buzzerStateStartTime < errorPauseTime)
                break;
        }

        buzzer_state = BUZZER_ERROR_ON;
        break;
    }
}

void startShortTone()
{
    buzzerRingCounter = 0;
    buzzer_state = BUZZER_NORMAL_ON;
}

void startLongTone()
{
    buzzerRingCounter = 0;
    buzzer_state = BUZZER_LONG_ON;
}

void startErrorTone()
{
    buzzerRingCounter = 0;
    buzzer_state = BUZZER_ERROR_ON;
}


void stopTone()
{
    digitalWrite(BUZZER, LOW);
    buzzerRingCounter = 0;
    buzzer_state = BUZZER_OFF;
}

static void startReminderPattern()
{
    switch (currentReminder)
    {
        case BuzzerReminder::ConfirmDispense: startShortTone(); break;
        case BuzzerReminder::RemoveMedication: startLongTone(); break;
        case BuzzerReminder::EmptyAfterPurge: startErrorTone(); break;
        case BuzzerReminder::None: break;
    }
}

void buzzerSetReminder(BuzzerReminder reminder)
{
    if (reminder == currentReminder) return;

    // Don't finish a stale alert after the patient has responded.
    stopTone();
    currentReminder = reminder;
    reminderStartedAt = millis();
    if (currentReminder != BuzzerReminder::None)
        startReminderPattern(); // First reminder immediately on entering the state.
}

void buzzerReminderExecute()
{
    if (currentReminder == BuzzerReminder::None) return;
    unsigned long now = millis();
    if (now - reminderStartedAt < REMINDER_INTERVAL_MS) return;

    reminderStartedAt = now;
    startReminderPattern();
}
