#include "DeviceApi.h"
#include "DeviceConfig.h"
#include "DeviceClock.h"
#include "DeviceEvent.h"
#include "LocalSchedule.h"
#include "Dispenser.h"
#include "Purge.h"
#include "Homing.h"
#include <WiFi.h>
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include "AzureRootCAs.h"
#include <ArduinoJson.h>
#include <atomic>
#include <freertos/FreeRTOS.h>
#include <freertos/queue.h>
#include <freertos/task.h>
#include <Preferences.h>


// Wi-Fi and device API credentials are loaded from NVS; no Secrets.h is needed.


// Only this FreeRTOS task makes HTTP calls. The main loop handles all mechanics.
enum CommandKind : uint8_t { EARLY_DISPENSE, MANUAL_PURGE };

struct DeviceCommand
{
    CommandKind kind;
    int64_t id;
    int64_t roundId;
    int version;
    uint8_t slot;
    char operationId[37]; // Scheduled slot operation, not the command operation
    char commandGuid[37];
};

struct CommandAck
{
    int64_t id;
    char commandGuid[37];
};

static QueueHandle_t commands = nullptr;
static QueueHandle_t acks = nullptr;
static std::atomic<bool> remoteAuthorized{false};
static std::atomic<unsigned long> lastHeartbeatOk{0};
static unsigned long lastWifiAttempt = 0;
static std::atomic<bool> reconnectCheckPending{true};
static std::atomic<unsigned long> connectedAt{0};
static unsigned long startedAt = 0;

bool deviceApiDispatchAllowed() {
    if (!reconnectCheckPending.load()) return true;
    if (millis() - startedAt < 10000UL) return false;
    if (WiFi.status() == WL_CONNECTED &&
        millis() - connectedAt.load() < 10000UL) return false;
    return true;
}

// Only the network task writes pairing information; the LCD reads a snapshot.
static portMUX_TYPE pairingMux = portMUX_INITIALIZER_UNLOCKED;
static char cachedPairingCode[7] = {};
static int64_t pairingExpiryUtc = 0;
static bool pairingIsAssigned = false;

bool devicePairingIsPaired()
{
    portENTER_CRITICAL(&pairingMux);
    const bool paired = pairingIsAssigned;
    portEXIT_CRITICAL(&pairingMux);
    return paired;
}

bool devicePairingCode(String& code)
{
    char snapshot[7];
    int64_t expires;
    bool paired;
    portENTER_CRITICAL(&pairingMux);
    memcpy(snapshot, cachedPairingCode, sizeof(snapshot));
    expires = pairingExpiryUtc;
    paired = pairingIsAssigned;
    portEXIT_CRITICAL(&pairingMux);

    const int64_t now = deviceClockNow();
    if (paired || snapshot[0] == '\0' || now <= 0 || now >= expires)
        return false;

    code = snapshot;
    return true;
}

static void updatePairingCode(bool paired, const String& code, int64_t expires)
{
    portENTER_CRITICAL(&pairingMux);
    pairingIsAssigned = paired;
    strlcpy(cachedPairingCode, code.c_str(), sizeof(cachedPairingCode));
    pairingExpiryUtc = expires;
    portEXIT_CRITICAL(&pairingMux);
}



static String wifiSsid;
static String wifiPassword;
static String deviceApiKey;

/*
static void saveCredentials()
{
    Preferences preferences;
    preferences.begin("medidrop", false);

    preferences.putString("wifiSsid", SETUP_WIFI_SSID);
    preferences.putString("wifiPass", SETUP_WIFI_PASSWORD);
    preferences.putString("apiKey", SETUP_DEVICE_API_KEY);

    preferences.end();

    Serial.println("[CONFIG] Credentials saved to NVS.");
}
*/

static void loadCredentials()
{
    Preferences preferences;
    preferences.begin("medidrop", true);

    wifiSsid = preferences.getString("wifiSsid", "");
    wifiPassword = preferences.getString("wifiPass", "");
    deviceApiKey = preferences.getString("apiKey", "");

    preferences.end();
}


static int request(const String& route, bool post, const String& body, String& response)
{
    if (WiFi.status() != WL_CONNECTED) return -1;
    // A valid UTC clock is required for TLS certificate expiry checks.
    if (!deviceClockValid()) return -1;

    WiFiClientSecure tls;
    tls.setCACert(AZURE_ROOT_CA_CERTIFICATES);
    tls.setTimeout(8000);
    HTTPClient http;
    http.setTimeout(8000); // Azure HTTPS may be slower than the local HTTP server.
    http.setFollowRedirects(HTTPC_STRICT_FOLLOW_REDIRECTS);
    if (!http.begin(tls, String(MEDIDROP_API_BASE) + route)) return -1;
    http.addHeader("X-Device-Key", deviceApiKey);
    if (post) http.addHeader("Content-Type", "application/json");
    int code = post ? http.POST(body) : http.GET();
    if (code > 0) response = http.getString();
    else Serial.printf("[HTTPS] %s failed: %s\n", route.c_str(), http.errorToString(code).c_str());
    http.end();
    return code;
}

static bool httpSuccess(int code)
{
    return code >= 200 && code < 300;
}

static void connectWifi()
{
    if (WiFi.status() == WL_CONNECTED)
        return;

    if (millis() - lastWifiAttempt < 8000)
        return;

    lastWifiAttempt = millis();

    Serial.printf(
        "[WiFi] Connecting to %s...\n",
        wifiSsid.c_str()
    );

    WiFi.mode(WIFI_STA);
    WiFi.begin(wifiSsid.c_str(), wifiPassword.c_str());
}

bool deviceApiOnline()
{
    unsigned long last = lastHeartbeatOk.load();
    return WiFi.status() == WL_CONNECTED && last != 0 && millis() - last < 45000;
}

static const char* operatingState()
{
    if (purgeBusy()) return "Purging";

    switch (dispenserStatus())
    {
        case INITIALIZING: return "Initializing";

        case IDLE: return "Idle";

        case WAIT_CONFIRM:
        case WAIT_CANCEL_HOLD: return "AwaitingDispenseConfirmation";

        case WAIT_FOR_HOMING: return "Homing";

        case ASSIGNED_TASK:
        case MOVE_TO_CONTAINER_POSITION: return "StartingTask";

        case SERVO_UP:
        case SERVO_DOWN:
        case MOVE_TO_RELEASE_POSITION: return "Dispensing";

        case WAIT_FOR_SCALE_READING:
        case WAIT_FOR_MEDICATION_REMOVAL: return "AwaitingMedicationRemoval";

        default: return "Syncing";
    }
}

static void heartbeat()
{
    JsonDocument payload;
    payload["operatingState"] = operatingState();
    String json, response;
    serializeJson(payload, json);
    if (!httpSuccess(request("/api/device/heartbeat", true, json, response))) return;
    lastHeartbeatOk = millis();
    JsonDocument result;
    if (deserializeJson(result, response)) return;
    int64_t serverSeconds = 0;
    if (parseIsoTime(result["serverTime"].as<String>(), serverSeconds))
        if (!deviceClockValid() || llabs(deviceClockNow() - serverSeconds) > 30)
            deviceClockSet(serverSeconds);
}

// The server generates and rotates the code. ESP32 only caches it for the LCD.
static void pollPairingCode()
{
    String response;
    if (request("/api/device/pairing-code", false, "", response) != 200)
        return;

    JsonDocument doc;
    if (deserializeJson(doc, response)) return;

    const String status = doc["status"].as<String>();
    if (status == "Paired")
    {
        updatePairingCode(true, "", 0);
        return;
    }

    if (status != "Unpaired") return;

    const String code = doc["code"].as<String>();
    int64_t expiresUtc = 0;
    if (code.length() != 6 || !parseIsoTime(doc["expiresAt"].as<String>(), expiresUtc))
        return;
    for (size_t i = 0; i < code.length(); ++i)
        if (code[i] < '0' || code[i] > '9') return;

    updatePairingCode(false, code, expiresUtc);
}

static void ackSchedule()
{
    if (scheduleRoundId() == 0 || scheduleAcknowledged()) return;
    JsonDocument ack;
    ack["medicationRoundId"] = (int64_t)scheduleRoundId();
    ack["scheduleVersion"] = scheduleVersion();
    String json, response;
    serializeJson(ack, json);
    if (httpSuccess(request("/api/device/schedule/ack", true, json, response)))
        scheduleSetAcknowledged(ack["medicationRoundId"].as<int64_t>(), ack["scheduleVersion"].as<int>());
}

static void pollSchedule()
{
    if (dispenserBusy() || scheduleIsClosing()) return;
    char roundText[32];
    snprintf(roundText, sizeof(roundText), "%lld", (long long)scheduleRoundId());
    String route = String("/api/device/schedule?roundId=") +
        roundText + "&version=" + String(scheduleVersion());
    String response;
    int code = request(route, false, "", response);
    if (code != 200)
    {
        if (code > 0) Serial.printf("[SCHEDULE] Poll HTTP %d: %s\n", code, response.c_str());
        ackSchedule();
        return;
    }
    JsonDocument doc;
    if (deserializeJson(doc, response))
    {
        ackSchedule();
        return;
    }
    if (doc["hasSchedule"].as<bool>()) {
        if (doc["changed"].as<bool>()) {
            if (scheduleInstall(doc.as<JsonVariantConst>()))
                Serial.println("[API] Schedule snapshot saved in flash");
            else {
                Serial.println("[SCHEDULE] Failed to install changed snapshot");
                return; // Do not pretend we reconciled an invalid snapshot.
            }
        }
        scheduleConfirmServerSnapshot(doc["medicationRoundId"].as<int64_t>(),
                                      doc["scheduleVersion"].as<int>());
    }
    reconnectCheckPending = false;
    ackSchedule(); // Retry the acknowledgement if the prior request failed.
}

static void pollCommands()
{
    if (!deviceApiOnline()) {
        static unsigned long lastOfflineLog = 0;
        if (millis() - lastOfflineLog > 15000) {
            lastOfflineLog = millis();
            Serial.println("[CMD] Skipping poll: API heartbeat not online");
        }
        return;
    }
    if (uxQueueSpacesAvailable(commands) == 0) {
        Serial.println("[CMD] Skipping poll: local command queue full");
        return;
    }
    String response;

    int code = request("/api/device/commands", false, "", response);

    if (code != 200) {
        Serial.printf("[CMD] GET commands HTTP %d | %s\n", code, response.c_str());
        return;
    }

    JsonDocument doc;
    if (deserializeJson(doc, response) || !doc.is<JsonArray>()) {
        Serial.printf("[CMD] Invalid commands JSON: %s\n", response.c_str());
        return;
    }
    if (doc.as<JsonArrayConst>().size() > 0)
        Serial.printf("[CMD] Received %u command(s)\n", (unsigned)doc.as<JsonArrayConst>().size());
    for (JsonVariantConst item : doc.as<JsonArrayConst>())
    {
        String type = item["type"].as<String>();
        if (type != "EarlyDispense" && type != "Dispense" && type != "Purge") {
            Serial.printf("[CMD] Ignored unsupported type '%s'\n", type.c_str());
            continue;
        }

        JsonDocument nested;
        JsonVariantConst payload = item["payload"];
        if (payload.is<const char*>())
        {
            if (deserializeJson(nested, payload.as<String>())) {
                Serial.println("[CMD] Failed to parse command payload string");
                continue;
            }
            payload = nested.as<JsonVariantConst>();
        }
        DeviceCommand command{};
        command.kind = type == "Purge" ? MANUAL_PURGE : EARLY_DISPENSE;
        command.id = item["id"].as<int64_t>();
        command.roundId = payload["medicationRoundId"].as<int64_t>();
        command.version = payload["scheduleVersion"].as<int>();
        command.slot = payload["deviceSlotNumber"].as<uint8_t>();
        strlcpy(command.operationId, payload["operationId"].as<String>().c_str(), sizeof(command.operationId));
        strlcpy(command.commandGuid, item["operationId"].as<String>().c_str(), sizeof(command.commandGuid));
        if (command.id == 0 || command.roundId != scheduleRoundId() ||
            command.version != scheduleVersion()) {
            Serial.printf("[CMD] Ignored %s id=%lld: command round=%lld v=%d, ESP round=%lld v=%d\n",
                type.c_str(), (long long)command.id, (long long)command.roundId, command.version,
                (long long)scheduleRoundId(), scheduleVersion());
            continue;
        }
        if (xQueueSend(commands, &command, 0) != pdTRUE) {
            Serial.println("[CMD] Local queue full while receiving commands");
            break;
        }
        Serial.printf("[CMD] Queued locally: %s id=%lld slot=%u\n",
            type.c_str(), (long long)command.id, (unsigned)command.slot);
    }
}

static void uploadAcks()
{
    CommandAck ack;
    if (xQueuePeek(acks, &ack, 0) != pdTRUE) return;
    JsonDocument doc;
    doc["commandOperationId"] = ack.commandGuid;
    String json, response;
    serializeJson(doc, json);
    char buffer[30];
    snprintf(buffer, sizeof(buffer), "%lld", (long long)ack.id);
    int code = request(String("/api/device/commands/") + buffer + "/ack", true, json, response);
    if (httpSuccess(code)) {
        Serial.printf("[CMD] ACK uploaded id=%lld\n", (long long)ack.id);
        xQueueReceive(acks, &ack, 0);
    } else {
        Serial.printf("[CMD] ACK failed id=%lld HTTP %d | %s\n",
            (long long)ack.id, code, response.c_str());
    }
}

static void uploadEvents()
{
    String json, response;
    if (!eventOldest(json)) return;
    int code = request("/api/device/events", true, json, response);
    if (!httpSuccess(code)) {
        if (code > 0) Serial.printf("[EVENTS] Upload HTTP %d: %s\n", code, response.c_str());
        // An old completion can conflict with slots added while offline.
        // Reopen the local round so a newer server snapshot can be installed.
        if (code == 409 && json.indexOf("\"ScheduleRoundCompleted\"") >= 0 &&
            response.indexOf("Not all round operations") >= 0) {
            JsonDocument rejected;
            if (!deserializeJson(rejected, json)) {
                scheduleRejectRoundCompletion(rejected["medicationRoundId"].as<int64_t>(),
                                              rejected["scheduleVersion"].as<int>());
                eventRemoveOldest();
                reconnectCheckPending = true;
            }
        }
        return;
    }
    JsonDocument event;
    if (!deserializeJson(event, json))
    {
        String eventType = event["eventType"].as<String>();
        int64_t roundId = event["medicationRoundId"].as<int64_t>();
        int version = event["scheduleVersion"].as<int>();
        // Transition first: prevents the main loop recreating this event in the
        // brief gap between removing it from the outbox and changing phase.
        scheduleEventUploaded(eventType, roundId, version);
        eventRemoveOldest();
    }
}

static void checkRemote()
{
    if (dispenserStatus() != WAIT_CONFIRM)
        return;

    String response;
    if (request("/api/device/remote-dispense", false, "", response) != 200)
        return;

    JsonDocument doc;
    if (!deserializeJson(doc, response) && doc["dispense"].as<bool>() &&
        dispenserStatus() == WAIT_CONFIRM)
        remoteAuthorized = true;
}

bool takeRemoteAuthorization() { return remoteAuthorized.exchange(false); }
void clearRemoteAuthorization() { remoteAuthorized = false; }

static void networkTask(void*)
{
    unsigned long heartbeatAt = 0, scheduleAt = 0, commandAt = 0, eventAt = 0, remoteAt = 0, pairingAt = 0;
    bool ntpConfigured = false;
    unsigned long lastClockLog = 0;
    bool wasConnected = false;
    for (;;)
    {
        if (WiFi.status() != WL_CONNECTED)
        {
            wasConnected = false;
            reconnectCheckPending = true;
            ntpConfigured = false;
            connectWifi();
            vTaskDelay(pdMS_TO_TICKS(250));
            continue;
        }
        // HTTP used to provide time via the server heartbeat; HTTPS requires
        // time *before* the first heartbeat to validate Azure's certificate.
        if (!ntpConfigured)
        {
            configTime(0, 0, "pool.ntp.org", "time.google.com");
            ntpConfigured = true;
            Serial.println("[HTTPS] Wi-Fi connected; syncing UTC clock via NTP");
        }
        if (!deviceClockValid())
        {
            if (millis() - lastClockLog >= 15000UL)
            {
                lastClockLog = millis();
                Serial.println("[HTTPS] Waiting for NTP clock; requests paused");
            }
            vTaskDelay(pdMS_TO_TICKS(250));
            continue;
        }
        unsigned long now = millis();
        if (!wasConnected) {
            wasConnected = true;
            connectedAt = now;
            reconnectCheckPending = true;
            scheduleAt = 0;
        }
        if (now - heartbeatAt >= HEARTBEAT_MS)
        {
            heartbeatAt = now;
            heartbeat();
        }
        // Fetch at startup and every 30 seconds. Does not rotate per request.
        if (pairingAt == 0 || now - pairingAt >= 30000UL)
        {
            pairingAt = now;
            pollPairingCode();
        }
        if (scheduleAt == 0 || now - scheduleAt >= SCHEDULE_POLL_MS ||
            (scheduleNeedsCompletionCheck() && eventsEmpty() &&
             now - scheduleAt >= 1000UL))
        {
            scheduleAt = now;
            pollSchedule();
        }
        if (now - commandAt >= COMMAND_POLL_MS)
        {
            commandAt = now;
            pollCommands();
        }
        uploadAcks();
        if (now - eventAt >= EVENT_UPLOAD_MS)
        {
            eventAt = now;
            uploadEvents();
        }
        if (dispenserStatus() == WAIT_CONFIRM && now - remoteAt >= REMOTE_DISPENSE_POLL_MS)
        {
            remoteAt = now;
            checkRemote();
        }
        vTaskDelay(pdMS_TO_TICKS(100));
    }
}

void deviceApiBegin()
{
    //Uncomment to update wifi and api key.
    //saveCredentials();

    loadCredentials();
    startedAt = millis();

    commands = xQueueCreate(4, sizeof(DeviceCommand));
    acks = xQueueCreate(8, sizeof(CommandAck));
    WiFi.mode(WIFI_STA);
    WiFi.begin(wifiSsid.c_str(), wifiPassword.c_str());
    lastWifiAttempt = millis();

    xTaskCreatePinnedToCore(
        networkTask,
        "MediDropHTTP",
        12000,
        nullptr,
        1,
        nullptr,
        0
    );
}

void deviceApiProcess()
{
    if (!deviceApiDispatchAllowed() || dispenserBusy() || !isHomed())
        return;

    DeviceCommand command;
    if (xQueueReceive(commands, &command, 0) != pdTRUE)
        return;

    if (command.roundId != scheduleRoundId() || command.version != scheduleVersion()) {
        Serial.println("[CMD] Dropped: schedule changed before execution");
        return;
    }

    Serial.printf("[CMD] Processing %s id=%lld homed=%d online=%d\n",
        command.kind == MANUAL_PURGE ? "Purge" : "EarlyDispense",
        (long long)command.id, (int)isHomed(), (int)deviceApiOnline());
    bool accepted = false;

    if (command.kind == EARLY_DISPENSE &&
        scheduleCanEarly(command.operationId, command.slot))
    {
        accepted = assignTask(command.operationId, command.slot);

        if (accepted) scheduleStart(command.operationId);
    }

    else if (command.kind == MANUAL_PURGE && deviceApiOnline() && scheduleCanManualPurge())
    {
        if (startPurge()) accepted = scheduleBeginManualPurge();
    }
    if (!accepted) {
        Serial.printf("[CMD] REJECTED id=%lld: earlyEligible=%d purgeEligible=%d\n",
            (long long)command.id,
            (int)scheduleCanEarly(String(command.operationId), command.slot),
            (int)scheduleCanManualPurge());
    }
    if (accepted)
    {
        Serial.printf("[CMD] ACCEPTED id=%lld; sending ack\n", (long long)command.id);
        CommandAck ack{};
        ack.id = command.id;
        strlcpy(ack.commandGuid, command.commandGuid, sizeof(ack.commandGuid));
        xQueueSend(acks, &ack, 0); // Acknowledged = accepted, not mechanically completed.
    }
}
