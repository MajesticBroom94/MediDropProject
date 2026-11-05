#pragma once

#include <Arduino.h>
#include <AccelStepper.h>
#include <LiquidCrystal_AIP31068_I2C.h>
#include <Servo.h>

#include "HX711.h"

// I2C
constexpr int I2C_SDA = 8;
constexpr int I2C_SCL = 9;

// HX711 Load Cell
constexpr int HX711_DOUT = 4;
constexpr int HX711_SCK = 5;

// Stepper
constexpr int STEPPER_STEP = 6;
constexpr int STEPPER_DIR = 7;
constexpr int STEPPER_ENABLE = 15;
constexpr int STEPPER_INCREMENT = 725;

// Indicators
constexpr int BUZZER = 38;
constexpr int LED1 = 1;
constexpr int LED2 = 2;

// Inputs
constexpr int LIMIT_SWITCH = 40;
constexpr int RIGHT_BTN = 41;
constexpr int LEFT_BTN = 42;

// Servos
constexpr int SERVO_1_PIN = 12;
constexpr int SERVO_2_PIN = 13;
constexpr int SERVO_3_PIN = 14;

constexpr double SERVO_SPEED = 45;
constexpr double SERVO_EASING = 0.70;

extern Servo servo;

extern LiquidCrystal_AIP31068_I2C lcd;

extern AccelStepper stepper;

extern int servoPins[3];

extern HX711 scale;

void hardwareBegin();

bool switchTriggered(int pin);

void scaleBegin();
bool scaleIsReady();
float scaleGetWeight();
void scaleTare();
void scaleSetCalibration(float factor);

