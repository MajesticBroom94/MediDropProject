#pragma once
#include <WString.h>

void deviceApiBegin();
void deviceApiProcess();      // Called from the main loop (no HTTP here)
bool takeRemoteAuthorization();
void clearRemoteAuthorization();
bool deviceApiOnline();

// Pairing code is updated by the network task and read by the LCD task.
bool devicePairingCode(String& code);
bool devicePairingIsPaired();
// Briefly hold locally due operations while reconnecting to retrieve edits.
bool deviceApiDispatchAllowed();