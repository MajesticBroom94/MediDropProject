#pragma once
#include <stdint.h>
bool startPurge();
void purgeSequenceExecute();
bool purgeBusy();
bool takePurgeCompleted();
uint8_t purgeCurrentGroup(); // 1..7; 0 means returning home
