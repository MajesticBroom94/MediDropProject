#pragma once
#include <Arduino.h>

// Unix seconds, always UTC. DS3231 stores UTC, not Johannesburg wall time.
bool deviceClockBegin();
bool deviceClockValid();
int64_t deviceClockNow();
void deviceClockSet(int64_t unixSeconds);
bool parseIsoTime(const String& iso, int64_t& unixSeconds);
String utcIsoTime(int64_t unixSeconds);
