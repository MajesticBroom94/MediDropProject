// Purge.cpp
#include "Purge.h"

#include "Dispenser.h"
#include "Hardware.h"
#include "Homing.h"
#include "Types.h"
#include <atomic>


static int dispensePosition;
static int slotGroup = 0;
static unsigned long stateStartTime = 0;
static std::atomic<PurgeProgres> purgeState{PURGE_IDLE};
static bool purgeCompleted = false;

bool startPurge()
{
    if (!dispenserBusy() && isHomed())
    {
        slotGroup = 0;
        purgeCompleted = false;
        purgeState = PURGE_RUN;
        return true;
    }
    return false;
}

void purgeSequenceExecute()
{
     switch (purgeState.load())
     {
     case PURGE_IDLE:
         break;


     case PURGE_RUN:
         {
             // allow homing to become available during operations
             unsetHomedPosition();
             dispensePosition = slotGroup * (STEPPER_INCREMENT + 3) * -1;
             stepper.moveTo(dispensePosition);
             purgeState = MOVE_TO_STEP;
         }
         break;

     case MOVE_TO_STEP:
         {
             if (stepper.distanceToGo() != 0)
                 stepper.run();
             else
                 purgeState = SERVOS_UP;
         }
         break;


     case SERVOS_UP:
         {
             bool allFinished = true;

             for (int i = 0; i < 3; i++)
             {
                 float progress = servo.write(servoPins[i], 78, SERVO_SPEED, SERVO_EASING);

                 if (progress < 1.0f)
                     allFinished = false;
             }

             if (allFinished)
             {
                 stateStartTime = millis();
                 purgeState = SERVOS_DOWN;
             }
         }
         break;

     case SERVOS_DOWN:
         {
             if (millis() - stateStartTime < 800)
                 break;

             bool allFinished = true;

             for (int i = 0; i < 3; i++)
             {

                 float progress = servo.write(servoPins[i], 0, SERVO_SPEED, SERVO_EASING);

                 if (progress > 0.0f)
                 {
                     allFinished = false;
                 }
             }

             if (allFinished)
             {
                 if (slotGroup < 6)
                 {
                     slotGroup++;
                     purgeState = PURGE_RUN;
                 }
                 else
                 {
                     slotGroup = 0;
                     startHoming();
                     purgeState = PURGE_ENDING;
                 }
             }
             break;
         }
     case PURGE_ENDING:
         if (isHomed())
         {
             purgeState = PURGE_IDLE;
             purgeCompleted = true;
         }
         break;
     }
}

bool purgeBusy()
{
    return purgeState != PURGE_IDLE;
}
bool takePurgeCompleted()
{
    if (!purgeCompleted) return false;
    purgeCompleted = false;
    return true;
}

uint8_t purgeCurrentGroup()
{
    if (purgeState == PURGE_ENDING) return 0;
    return static_cast<uint8_t>(slotGroup + 1);
}
