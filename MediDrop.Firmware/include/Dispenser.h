#pragma once
#include "Types.h"
#include <Arduino.h>
void dispenserSequenceExecute();
bool assignTask(const char* operationId, uint8_t slot);
Status dispenserStatus();
bool dispenserBusy();
uint8_t dispenserCurrentSlot();
