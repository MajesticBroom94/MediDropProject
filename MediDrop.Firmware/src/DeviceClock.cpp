#include "DeviceClock.h"
#include "Hardware.h"
#include <sys/time.h>
#include <Arduino.h>
#include <cstdlib>
#include <time.h>

bool deviceClockBegin()
{
    setenv("TZ", "UTC0", 1);
    tzset();

    return true;
}

bool deviceClockValid()
{
    return time(nullptr) > 1700000000;
}

int64_t deviceClockNow()
{
    if (!deviceClockValid())
        return 0;

    return static_cast<int64_t>(time(nullptr));
}

void deviceClockSet(int64_t unixSeconds)
{
    if (unixSeconds <= 1700000000)
        return;

    timeval tv{};
    tv.tv_sec = static_cast<time_t>(unixSeconds);
    settimeofday(&tv, nullptr);
}


bool parseIsoTime(const String& iso, int64_t& out) {
    int y=0, mo=0, d=0, h=0, mi=0, sec=0;
    if (sscanf(iso.c_str(), "%d-%d-%dT%d:%d:%d", &y, &mo, &d, &h, &mi, &sec) != 6)
        return false;
    if (y < 2020 || mo < 1 || mo > 12 || d < 1 || d > 31 || h > 23 || mi > 59 || sec > 59)
        return false;
    tm utc{};
    utc.tm_year = y-1900; utc.tm_mon = mo-1; utc.tm_mday = d;
    utc.tm_hour = h; utc.tm_min = mi; utc.tm_sec = sec;
    int64_t unixTime = mktime(&utc);
    size_t pos=19;
    if (pos < iso.length() && iso[pos] == '.') {
        ++pos;
        while (pos < iso.length() && isdigit((unsigned char)iso[pos])) ++pos;
    }
    int offset = 0;
    if (pos < iso.length() && (iso[pos] == '+' || iso[pos] == '-')) {
        int hours=0, minutes=0;
        if (sscanf(iso.c_str()+pos+1, "%d:%d", &hours, &minutes) != 2 || hours > 23 || minutes > 59)
            return false;
        offset = (hours*3600 + minutes*60) * (iso[pos] == '+' ? 1 : -1);
    } else if (pos < iso.length() && iso[pos] != 'Z') return false;
    out = unixTime - offset;
    return true;
}

String utcIsoTime(int64_t unixSeconds) {
    time_t t = (time_t)unixSeconds;
    tm utc{};
    gmtime_r(&t, &utc);
    char text[28];
    strftime(text, sizeof(text), "%Y-%m-%dT%H:%M:%SZ", &utc);
    return String(text);
}
