#include "../include/Hardware.h"
#include <Arduino.h>

Servo servo;

HX711 scale;

LiquidCrystal_AIP31068_I2C lcd(
    0x3E,
    16,
    2
);

AccelStepper stepper(
    AccelStepper::DRIVER,
    STEPPER_STEP,
    STEPPER_DIR
);

int servoPins[3] = {
    SERVO_1_PIN,
    SERVO_2_PIN,
    SERVO_3_PIN
};

bool switchTriggered(int pin)
{
    if (digitalRead(pin) != LOW)
        return false;

    delayMicroseconds(500);

    return digitalRead(pin) == LOW;
}

void hardwareBegin()
{
    pinMode(STEPPER_STEP, OUTPUT);
    pinMode(STEPPER_DIR, OUTPUT);
    pinMode(STEPPER_ENABLE, OUTPUT);

    digitalWrite(STEPPER_ENABLE, LOW);

    pinMode(LIMIT_SWITCH, INPUT_PULLUP);
    pinMode(RIGHT_BTN, INPUT_PULLUP);
    pinMode(LEFT_BTN, INPUT_PULLUP);

    pinMode(LED1, OUTPUT);
    pinMode(LED2, OUTPUT);
    pinMode(BUZZER, OUTPUT);

    digitalWrite(BUZZER, LOW);
    digitalWrite(LED1, LOW);
    digitalWrite(LED2, LOW); // RED LED

    servo.attach(SERVO_1_PIN, 500, 2500);
    servo.attach(SERVO_2_PIN, 500, 2500);
    servo.attach(SERVO_3_PIN, 500, 2500);

    // set safe position
    servo.write(SERVO_1_PIN, 0);
    delay(300);
    servo.write(SERVO_2_PIN, 0);
    delay(300);
    servo.write(SERVO_3_PIN, 0);
    delay(500);

    // stepper config
    stepper.setMaxSpeed(2000);       // steps/second
    stepper.setAcceleration(2000);   // steps/second
}

static float scaleCalibrationFactor = 4968.64f;

void scaleBegin()
{
    scale.begin(HX711_DOUT, HX711_SCK);

    if (!scale.is_ready())
    {
        Serial.println("[SCALE] HX711 not detected.");
        return;
    }

    scale.set_scale(scaleCalibrationFactor);

    Serial.println("[SCALE] Initialized.");
}

bool scaleIsReady()
{
    return scale.is_ready();
}

float scaleGetWeight()
{
    if (!scaleIsReady())
        return 0.0f;

    return scale.get_units(1);
}

void scaleTare()
{
    if (!scaleIsReady())
        return;

    scale.tare(5);
}

void scaleSetCalibration(float factor)
{
    if (factor == 0.0f)
        return;

    scaleCalibrationFactor = factor;
    scale.set_scale(factor);
}

