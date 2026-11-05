#pragma once
#include <Arduino.h>

// Seven expected outcomes. ASP.NET decides what each event means.
enum class DeviceEventType {
    DispenseCompleted,
    DispenseCancelled,
    DispenseTimedOut,
    MedicationCollected,
    MedicationCollectionTimedOut,
    ScheduleRoundCompleted,
    ManualPurgeCompleted
};

void eventsBegin();
bool queueDeviceEvent(DeviceEventType type, const String& operationId = "");
bool eventOldest(String& json);
void eventRemoveOldest();
bool eventsEmpty();
const char* deviceEventName(DeviceEventType type);

// Used to recover persisted closing phases after a reboot.
bool eventQueuedForRound(DeviceEventType type, long long roundId, int version);
