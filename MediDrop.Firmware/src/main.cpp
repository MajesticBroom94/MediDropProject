#include <Arduino.h>
#include <LittleFS.h>
#include <Wire.h>
#include "Hardware.h"
#include "Buzzer.h"
#include "Homing.h"
#include "Dispenser.h"
#include "Purge.h"
#include "DeviceClock.h"
#include "LocalSchedule.h"
#include "DeviceEvent.h"
#include "DeviceApi.h"
#include "LCD.h"

void setup() {
    Serial.begin(115200);
    delay(1000);

    if (!LittleFS.begin(false))
    {
        Serial.println("[RECOVERY] LittleFS mount failed");
    }

    Wire.begin(I2C_SDA, I2C_SCL);
    hardwareBegin();           // Existing tested pins, servos, stepper and buttons
    lcd.init();
    deviceClockBegin();
    scaleBegin();

    if (!scheduleBegin()) Serial.println("[STARTUP] Could not load local schedule");
    eventsBegin();
    deviceApiBegin();
}

void loop() {
    homingSequenceExecute();
    dispenserSequenceExecute();
    purgeSequenceExecute();

    deviceApiProcess();        // Consume received commands on the main loop
    scheduleLifecycle();       // Round completion and purge transitions
    scheduleExecute();

    lcdPairingButtonExecute(); // Right-button long press only while idle
    lcdExecute();              // LEDs + LCD + reminder selection (never blocks)
    buzzerReminderExecute();   // Repeat alert patterns once per minute
    buzzerToneExecute();       // Non-blocking buzzer state machine
}
