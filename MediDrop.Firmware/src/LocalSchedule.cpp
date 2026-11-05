#include "LocalSchedule.h"
#include "DeviceClock.h"
#include "DeviceEvent.h"
#include "Dispenser.h"
#include "Homing.h"
#include "Purge.h"
#include "LCD.h"
#include "DeviceApi.h"
#include <LittleFS.h>
#include <freertos/FreeRTOS.h>
#include <freertos/semphr.h>

// A single retained round is the sole source of dispensing decisions.
// The phase also persists so a completed round is not dispatched again.
enum RoundPhase : uint8_t {
    ACTIVE = 0,
    ROUND_EVENT_QUEUED = 1,
    AUTO_PURGE_PENDING = 2,
    AUTO_PURGING = 3,
    MANUAL_PURGING = 4,
    MANUAL_EVENT_QUEUED = 5,
    MANUAL_PURGE_PENDING = 6 // Recovery: restart an interrupted manual purge from home
};

static SemaphoreHandle_t scheduleMutex = nullptr;
static long long roundId = 0;
static int version = 0;
static bool acknowledged = false;
static RoundPhase phase = ACTIVE;
static ScheduledSlot slots[21];
static int slotCount = 0;
// RAM-only. After reboot or the final slot, confirm the latest server version.
static bool completionCheckNeeded = true;

// Do not start an operation more than an hour after its scheduled time.
// This matches the existing one-hour dispense-confirmation window.
static constexpr int64_t MAX_DISPENSE_LATENESS_SECONDS = 60LL * 60LL;

static bool saveSchedule() {
    JsonDocument doc;
    doc["medicationRoundId"] = (int64_t)roundId;
    doc["scheduleVersion"] = version;
    doc["acknowledged"] = acknowledged;
    doc["phase"] = (int)phase;
    JsonArray array = doc["slots"].to<JsonArray>();
    for (int i=0; i<slotCount; ++i) {
        JsonObject s = array.add<JsonObject>();
        s["deviceSlotNumber"] = slots[i].number;
        s["operationId"] = slots[i].operationId;
        s["dueUtc"] = slots[i].dueUtc;
        s["started"] = slots[i].started;
        s["finished"] = slots[i].finished;
    }
    File file = LittleFS.open("/schedule.tmp", "w");
    if (!file) return false;
    serializeJson(doc, file);
    file.close();
    LittleFS.remove("/schedule.json");
    return LittleFS.rename("/schedule.tmp", "/schedule.json");
}

bool scheduleBegin() {
    scheduleMutex = xSemaphoreCreateMutex();
    if (!LittleFS.begin(true)) return false;
    File file = LittleFS.open("/schedule.json", "r");
    if (!file) return true;
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, file);
    file.close();
    if (error) return false;
    roundId = doc["medicationRoundId"].as<int64_t>();
    version = doc["scheduleVersion"].as<int>();
    acknowledged = doc["acknowledged"].as<bool>();
    phase = static_cast<RoundPhase>(doc["phase"] | 0);
    // The motor/purge state is not persisted. A reboot during purge leaves
    // phase=*_PURGING but the mechanics idle. Start again only after homing.
    if (phase == AUTO_PURGING) phase = AUTO_PURGE_PENDING;
    else if (phase == MANUAL_PURGING) phase = MANUAL_PURGE_PENDING;
    // Persist recovery marker only AFTER restoring the saved slots below.
    const bool recoveredPurge = phase == AUTO_PURGE_PENDING || phase == MANUAL_PURGE_PENDING;
    JsonArrayConst array = doc["slots"].as<JsonArrayConst>();
    slotCount = 0;
    for (JsonVariantConst value : array) {
        if (slotCount >= 21) break;
        ScheduledSlot& s = slots[slotCount++];
        s.number = value["deviceSlotNumber"].as<uint8_t>();
        s.operationId = value["operationId"].as<String>();
        s.dueUtc = value["dueUtc"].as<int64_t>();
        s.started = value["started"].as<bool>();
        s.finished = value["finished"].as<bool>();
    }
    if (recoveredPurge && !saveSchedule())
        Serial.println("[RECOVERY] Could not persist interrupted purge marker");
    Serial.printf("[SCHEDULE] Restored round=%lld version=%d slots=%d phase=%d\n",
                  roundId, version, slotCount, (int)phase);
    return true;
}

void scheduleClear() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    roundId = 0; version = 0; slotCount = 0;
    acknowledged = false; phase = ACTIVE;
    completionCheckNeeded = true;
    saveSchedule();
    xSemaphoreGive(scheduleMutex);
    Serial.println("[SCHEDULE] Local round cleared");
}

long long scheduleRoundId() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    long long result = roundId;
    xSemaphoreGive(scheduleMutex);
    return result;
}

int scheduleVersion() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    int result = version;
    xSemaphoreGive(scheduleMutex);
    return result;
}

bool scheduleAcknowledged() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool result = acknowledged;
    xSemaphoreGive(scheduleMutex);
    return result;
}

void scheduleEventContext(long long& id, int& v) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    id = roundId; v = version;
    xSemaphoreGive(scheduleMutex);
}

bool scheduleInstall(JsonVariantConst doc) {
    long long incomingRound = doc["medicationRoundId"].as<int64_t>();
    int incomingVersion = doc["scheduleVersion"].as<int>();
    JsonArrayConst array = doc["slots"].as<JsonArrayConst>();
    if (incomingRound <= 0 || incomingVersion < 1 || array.isNull() || array.size() > 21)
        return false;

    ScheduledSlot incoming[21];
    int n = 0;
    for (JsonVariantConst value : array) {
        int number = value["deviceSlotNumber"].as<int>();
        String op = value["operationId"].as<String>();
        int64_t due = 0;
        if (number < 1 || number > 21 || op.length() != 36 ||
            !parseIsoTime(value["scheduledFor"].as<String>(), due)) return false;
        for (int i=0; i<n; ++i)
            if (incoming[i].number == number || incoming[i].operationId == op) return false;
        ScheduledSlot& slot = incoming[n++];
        slot.number = static_cast<uint8_t>(number);
        slot.operationId = op;
        slot.dueUtc = due;
        slot.started = false;
        slot.finished = false;
    }

    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (phase != ACTIVE || (roundId != 0 && roundId != incomingRound) ||
        (roundId == incomingRound && incomingVersion <= version)) {
        xSemaphoreGive(scheduleMutex);
        return false;
    }
    if (roundId == incomingRound) {
        for (int i=0; i<n; ++i)
            for (int j=0; j<slotCount; ++j)
                if (incoming[i].operationId == slots[j].operationId) {
                    incoming[i].started = slots[j].started;
                    incoming[i].finished = slots[j].finished;
                }
    }
    roundId = incomingRound;
    version = incomingVersion;
    slotCount = n;
    acknowledged = false;
    completionCheckNeeded = true;
    for (int i=0; i<n; ++i) slots[i] = incoming[i];
    bool ok = saveSchedule();
    xSemaphoreGive(scheduleMutex);
    if (ok) Serial.printf("[SCHEDULE] Installed version %d\n", incomingVersion);
    return ok;
}

void scheduleSetAcknowledged(long long id, int v) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (roundId == id && version == v && !acknowledged) {
        acknowledged = true;
        saveSchedule();
    }
    xSemaphoreGive(scheduleMutex);
}

bool scheduleNextDue(ScheduledSlot& result) {
    if (!deviceClockValid()) return false;
    int64_t now = deviceClockNow();
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    int chosen = -1;
    if (phase == ACTIVE)
        for (int i=0; i<slotCount; ++i)
            if (!slots[i].started && !slots[i].finished && slots[i].dueUtc <= now &&
                (chosen == -1 || slots[i].dueUtc < slots[chosen].dueUtc)) chosen = i;
    if (chosen >= 0) result = slots[chosen];
    xSemaphoreGive(scheduleMutex);
    return chosen >= 0;
}

bool scheduleNextPending(ScheduledSlot& result) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    int next = -1;
    if (phase == ACTIVE)
        for (int i = 0; i < slotCount; ++i)
            if (!slots[i].started && !slots[i].finished &&
                (next == -1 || slots[i].dueUtc < slots[next].dueUtc))
                next = i;
    if (next >= 0) result = slots[next];
    xSemaphoreGive(scheduleMutex);
    return next >= 0;
}

bool scheduleContains(const String& op, uint8_t number) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool found = false;
    if (phase == ACTIVE)
        for (int i=0; i<slotCount; ++i)
            if (slots[i].operationId == op && slots[i].number == number &&
                !slots[i].started && !slots[i].finished) { found=true; break; }
    xSemaphoreGive(scheduleMutex);
    return found;
}

bool scheduleCanEarly(const String& op, uint8_t number) {
    if (!deviceClockValid()) return false;
    int64_t now = deviceClockNow();
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    int next = -1;
    if (phase == ACTIVE) {
        for (int i=0; i<slotCount; ++i)
            if (!slots[i].started && !slots[i].finished && slots[i].dueUtc > now &&
                (next == -1 || slots[i].dueUtc < slots[next].dueUtc)) next = i;
    }
    bool valid = next >= 0 && slots[next].operationId == op && slots[next].number == number &&
        slots[next].dueUtc - now <= 2LL * 60LL * 60LL;
    xSemaphoreGive(scheduleMutex);
    return valid;
}

bool scheduleStart(const String& op) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool result = false;
    if (phase == ACTIVE)
        for (int i=0; i<slotCount; ++i)
            if (slots[i].operationId == op && !slots[i].started && !slots[i].finished) {
                slots[i].started = true;
                result = saveSchedule();
                break;
            }
    xSemaphoreGive(scheduleMutex);
    return result;
}

void scheduleFinish(const String& op) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    for (int i=0; i<slotCount; ++i)
        if (slots[i].operationId == op && !slots[i].finished) {
            slots[i].finished = true;
            slots[i].started = false;
            completionCheckNeeded = true;
            saveSchedule();
            break;
        }
    xSemaphoreGive(scheduleMutex);
}

bool scheduleNeedsCompletionCheck() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool result = roundId > 0 && phase == ACTIVE && acknowledged && completionCheckNeeded;
    if (result)
        for (int i = 0; i < slotCount; ++i)
            if (!slots[i].finished) { result = false; break; }
    xSemaphoreGive(scheduleMutex);
    return result;
}

void scheduleConfirmServerSnapshot(long long id, int v) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (roundId == id && version == v && phase == ACTIVE)
        completionCheckNeeded = false;
    xSemaphoreGive(scheduleMutex);
}

void scheduleRejectRoundCompletion(long long id, int v) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (roundId == id && version == v && phase == ROUND_EVENT_QUEUED) {
        phase = ACTIVE;
        completionCheckNeeded = true;
        saveSchedule();
        Serial.println("[SCHEDULE] Completion rejected; reopening to download newer schedule");
    }
    xSemaphoreGive(scheduleMutex);
}

bool scheduleIsClosing() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool result = phase != ACTIVE;
    xSemaphoreGive(scheduleMutex);
    return result;
}

bool scheduleCanManualPurge() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    bool allowed = roundId > 0 && acknowledged && phase == ACTIVE;
    xSemaphoreGive(scheduleMutex);
    return allowed;
}

bool scheduleBeginManualPurge() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (roundId == 0 || !acknowledged || phase != ACTIVE) {
        xSemaphoreGive(scheduleMutex);
        return false;
    }
    phase = MANUAL_PURGING;
    saveSchedule();
    xSemaphoreGive(scheduleMutex);
    return true;
}

void schedulePurgeEnded() {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    RoundPhase ended = phase;
    xSemaphoreGive(scheduleMutex);
    if (ended == AUTO_PURGING) {
        scheduleClear(); // No event: this was only physical cleanup of an already-completed round.
        lcdNotifyAutoPurgeComplete(); // Red LED and repeating empty alert.
    } else if (ended == MANUAL_PURGING) {
        xSemaphoreTake(scheduleMutex, portMAX_DELAY);
        phase = MANUAL_EVENT_QUEUED;
        saveSchedule();
        xSemaphoreGive(scheduleMutex);
        if (!queueDeviceEvent(DeviceEventType::ManualPurgeCompleted)) {
            xSemaphoreTake(scheduleMutex, portMAX_DELAY);
            phase = MANUAL_PURGING;
            saveSchedule();
            xSemaphoreGive(scheduleMutex);
        }
    }
}

void scheduleEventUploaded(const String& eventType, long long id, int v) {
    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (id == roundId && v == version) {
        if (eventType == "ScheduleRoundCompleted" && phase == ROUND_EVENT_QUEUED) {
            phase = AUTO_PURGE_PENDING;
            saveSchedule();
        } else if (eventType == "ManualPurgeCompleted" && phase == MANUAL_EVENT_QUEUED) {
            // Clear only after ASP has accepted the manual purge event.
            roundId = 0; version = 0; slotCount = 0;
            acknowledged = false; phase = ACTIVE;
            saveSchedule();
        }
    }
    xSemaphoreGive(scheduleMutex);
}

void scheduleExecute() {
    // Never make time-dependent decisions before NTP/server time is available.
    if (!deviceClockValid()) return;

    // Expire overdue operations even while another dispense or purge is busy.
    // Process one per loop to avoid holding the schedule mutex while enqueueing.
    ScheduledSlot stale;
    bool foundStale = false;
    const int64_t now = deviceClockNow();

    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    if (phase == ACTIVE) {
        for (int i = 0; i < slotCount; ++i) {
            const ScheduledSlot& candidate = slots[i];
            if (!candidate.started && !candidate.finished &&
                candidate.dueUtc <= now &&
                now - candidate.dueUtc > MAX_DISPENSE_LATENESS_SECONDS) {
                stale = candidate;
                foundStale = true;
                break;
            }
        }
    }
    xSemaphoreGive(scheduleMutex);

    if (foundStale) {
        // Use the existing timeout event understood by the ASP.NET event API.
        // Queue before marking finished so a failed queue can retry next loop.
        if (queueDeviceEvent(DeviceEventType::DispenseTimedOut, stale.operationId)) {
            scheduleFinish(stale.operationId);
            Serial.printf("[SCHEDULE] Missed overdue slot %u (operation %s); no dispense\n",
                          stale.number, stale.operationId.c_str());
        } else {
            Serial.println("[SCHEDULE] Could not queue missed-dose event; will retry");
        }
        return;
    }

    if (!deviceApiDispatchAllowed() || dispenserBusy() || !isHomed()) return;
    ScheduledSlot due;
    if (!scheduleNextDue(due)) return;
    if (assignTask(due.operationId.c_str(), due.number)) {
        if (!scheduleStart(due.operationId))
            Serial.println("[SCHEDULE] Could not retain started flag");
    }
}

void scheduleLifecycle() {
    if (takePurgeCompleted()) schedulePurgeEnded();

    xSemaphoreTake(scheduleMutex, portMAX_DELAY);
    RoundPhase currentPhase = phase;
    bool canComplete = roundId > 0 && phase == ACTIVE && acknowledged;
    if (canComplete)
        for (int i=0; i<slotCount; ++i)
            if (!slots[i].finished) { canComplete = false; break; }
    xSemaphoreGive(scheduleMutex);

    if (canComplete && !dispenserBusy() && eventsEmpty()) {
        xSemaphoreTake(scheduleMutex, portMAX_DELAY);
        bool marked = phase == ACTIVE;
        if (marked && !completionCheckNeeded) { phase = ROUND_EVENT_QUEUED; saveSchedule(); }
        else marked = false;
        xSemaphoreGive(scheduleMutex);
        if (marked && !queueDeviceEvent(DeviceEventType::ScheduleRoundCompleted)) {
            xSemaphoreTake(scheduleMutex, portMAX_DELAY);
            if (phase == ROUND_EVENT_QUEUED) { phase = ACTIVE; saveSchedule(); }
            xSemaphoreGive(scheduleMutex);
        }
    }
    // If flash contains a queued phase but the event outbox was lost between
    // writes or across power loss, recreate the event instead of hanging.
    if (currentPhase == ROUND_EVENT_QUEUED && scheduleIsClosing() &&
        !eventQueuedForRound(DeviceEventType::ScheduleRoundCompleted, scheduleRoundId(), scheduleVersion())) {
        if (queueDeviceEvent(DeviceEventType::ScheduleRoundCompleted))
            Serial.println("[RECOVERY] Restored round-completion event");
    }
    if (currentPhase == MANUAL_EVENT_QUEUED &&
        !eventQueuedForRound(DeviceEventType::ManualPurgeCompleted, scheduleRoundId(), scheduleVersion())) {
        if (queueDeviceEvent(DeviceEventType::ManualPurgeCompleted))
            Serial.println("[RECOVERY] Restored manual-purge event");
    }
    if ((currentPhase == AUTO_PURGE_PENDING || currentPhase == MANUAL_PURGE_PENDING) &&
        !dispenserBusy() && isHomed()) {
        if (startPurge()) {
            xSemaphoreTake(scheduleMutex, portMAX_DELAY);
            phase = currentPhase == AUTO_PURGE_PENDING ? AUTO_PURGING : MANUAL_PURGING;
            saveSchedule();
            xSemaphoreGive(scheduleMutex);
            Serial.println("[RECOVERY] Purge started/resumed after homing");
        }
    }
}