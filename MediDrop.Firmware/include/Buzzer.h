#pragma once

// The existing tone state machine produces the actual sound patterns.
void buzzerToneExecute();
void startShortTone();
void startLongTone();
void startErrorTone();
void stopTone();

enum class BuzzerReminder {
    None,
    ConfirmDispense,  // Three short beeps, repeat while awaiting confirmation
    RemoveMedication, // Long beep, repeat while awaiting collection
    EmptyAfterPurge    // Error pattern, repeat until a new round arrives
};

// Call from the main loop. Nothing here blocks or waits.
void buzzerSetReminder(BuzzerReminder reminder);
void buzzerReminderExecute();
