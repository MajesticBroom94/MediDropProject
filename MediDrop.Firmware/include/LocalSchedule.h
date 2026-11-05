#pragma once
#include <Arduino.h>
#include <ArduinoJson.h>

struct ScheduledSlot {
    uint8_t number = 0;
    String operationId;
    int64_t dueUtc = 0;
    bool started = false;
    bool finished = false;
};

bool scheduleBegin();
bool scheduleInstall(JsonVariantConst snapshot);
void scheduleClear();
long long scheduleRoundId();
int scheduleVersion();
bool scheduleAcknowledged();
void scheduleSetAcknowledged(long long roundId, int version);
bool scheduleNextDue(ScheduledSlot& slot);
bool scheduleNextPending(ScheduledSlot& slot); // For display only
bool scheduleContains(const String& operationId, uint8_t number);
bool scheduleCanEarly(const String& operationId, uint8_t number);
bool scheduleStart(const String& operationId);
void scheduleFinish(const String& operationId);
void scheduleEventContext(long long& roundId, int& version);

bool scheduleCanManualPurge();
bool scheduleBeginManualPurge();
void schedulePurgeEnded();
void scheduleEventUploaded(const String& eventType, long long roundId, int version);
void scheduleExecute();      // Due-time dispatch, runs on the main loop
void scheduleLifecycle();    // Round completion, auto purge, cleanup
bool scheduleIsClosing();

// A closing event must only be sent after a fresh successful schedule check.
bool scheduleNeedsCompletionCheck();
void scheduleConfirmServerSnapshot(long long roundId, int version);
// If ASP rejects completion because more work exists, allow schedule reconciliation.
void scheduleRejectRoundCompletion(long long roundId, int version);