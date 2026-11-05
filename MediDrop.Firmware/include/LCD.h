#pragma once
#include <Arduino.h>

// The LCD/LED presentation runs on the main loop, never the network task.
void lcdExecute();

enum class LcdNotice {
    DispenseConfirmed,
    DispenseCancelled,
    DispenseTimedOut,
    MedicationCollected,
    CollectionTimedOut
};

// Brief message displayed over the ordinary state screen (about 2.5 seconds).
void lcdNotify(LcdNotice notice, uint8_t slotNumber);

// Called only when the finished round's automatic purge has completed.
// Arms the repeating 'empty' error pattern until a new round arrives.
void lcdNotifyAutoPurgeComplete();

bool lcdEmptyAlertActive();
void lcdDismissEmptyAlert();
void lcdPairingButtonExecute();