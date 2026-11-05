#include "DeviceEvent.h"
#include "LocalSchedule.h"
#include "DeviceClock.h"
#include <ArduinoJson.h>
#include <LittleFS.h>
#include <esp_system.h>
#include <freertos/FreeRTOS.h>
#include <freertos/semphr.h>

static SemaphoreHandle_t eventMutex = nullptr;
static JsonDocument entries;
static constexpr size_t MAX_EVENTS = 64; // 21 slots x two events + room for round events

const char* deviceEventName(DeviceEventType type) {
    switch (type) {
        case DeviceEventType::DispenseCompleted: return "DispenseCompleted";
        case DeviceEventType::DispenseCancelled: return "DispenseCancelled";
        case DeviceEventType::DispenseTimedOut: return "DispenseTimedOut";
        case DeviceEventType::MedicationCollected: return "MedicationCollected";
        case DeviceEventType::MedicationCollectionTimedOut: return "MedicationCollectionTimedOut";
        case DeviceEventType::ScheduleRoundCompleted: return "ScheduleRoundCompleted";
        case DeviceEventType::ManualPurgeCompleted: return "ManualPurgeCompleted";
    }
    return "Unknown";
}

static bool saveEvents() {
    File f = LittleFS.open("/events.tmp", "w");
    if (!f) return false;
    serializeJson(entries, f);
    f.close();
    LittleFS.remove("/events.json");
    return LittleFS.rename("/events.tmp", "/events.json");
}

static String newGuid() {
    uint8_t b[16];
    esp_fill_random(b, sizeof(b));
    b[6] = (b[6] & 0x0f) | 0x40;
    b[8] = (b[8] & 0x3f) | 0x80;
    char s[37];
    snprintf(
    s,
    sizeof(s),
    "%02x%02x%02x%02x-"
    "%02x%02x-"
    "%02x%02x-"
    "%02x%02x-"
    "%02x%02x%02x%02x%02x%02x",
    b[0], b[1], b[2], b[3],
    b[4], b[5],
    b[6], b[7],
    b[8], b[9],
    b[10], b[11], b[12], b[13], b[14], b[15]
);
    return String(s);
}

void eventsBegin() {
    eventMutex = xSemaphoreCreateMutex();
    File f = LittleFS.open("/events.json", "r");
    if (f) {
        if (deserializeJson(entries, f)) entries.clear();
        f.close();
    }
    if (!entries.is<JsonArray>()) entries.to<JsonArray>();
}

bool queueDeviceEvent(DeviceEventType type, const String& operationId) {
    long long roundId;
    int version;
    scheduleEventContext(roundId, version);
    if (roundId <= 0 || version <= 0) return false;

    xSemaphoreTake(eventMutex, portMAX_DELAY);
    JsonArray array = entries.as<JsonArray>();
    if (array.size() >= MAX_EVENTS) {
        Serial.println("[EVENTS] Outbox full, cannot queue event");
        xSemaphoreGive(eventMutex);
        return false;
    }
    JsonObject e = array.add<JsonObject>();
    e["eventId"] = newGuid();
    e["eventType"] = deviceEventName(type);
    e["medicationRoundId"] = (int64_t)roundId;
    e["scheduleVersion"] = version;
    if (operationId.length()) e["operationId"] = operationId;
    e["occurredAt"] = utcIsoTime(deviceClockNow());
    bool ok = saveEvents();
    if (!ok) array.remove(array.size() - 1);
    xSemaphoreGive(eventMutex);
    return ok;
}

bool eventOldest(String& json) {
    xSemaphoreTake(eventMutex, portMAX_DELAY);
    JsonArray array = entries.as<JsonArray>();
    bool found = !array.isNull() && array.size() > 0;
    if (found) serializeJson(array[0], json);
    xSemaphoreGive(eventMutex);
    return found;
}

void eventRemoveOldest() {
    xSemaphoreTake(eventMutex, portMAX_DELAY);
    JsonArray array = entries.as<JsonArray>();
    if (!array.isNull() && array.size()) {
        array.remove(0);
        saveEvents();
    }
    xSemaphoreGive(eventMutex);
}

bool eventsEmpty() {
    xSemaphoreTake(eventMutex, portMAX_DELAY);
    bool empty = entries.as<JsonArray>().size() == 0;
    xSemaphoreGive(eventMutex);
    return empty;
}

bool eventQueuedForRound(DeviceEventType type, long long roundId, int version) {
    xSemaphoreTake(eventMutex, portMAX_DELAY);
    bool found = false;
    for (JsonVariantConst event : entries.as<JsonArrayConst>()) {
        if (event["eventType"].as<String>() == deviceEventName(type) &&
            event["medicationRoundId"].as<int64_t>() == roundId &&
            event["scheduleVersion"].as<int>() == version) {
            found = true;
            break;
        }
    }
    xSemaphoreGive(eventMutex);
    return found;
}
