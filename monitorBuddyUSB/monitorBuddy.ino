/*
 Name:		monitorBuddy.ino
 Created:	12/23/2020 7:51:59 PM
 Author:	glocal
*/

#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/wdt.h>

#include "MonitorBuddyApp.h"

MonitorBuddyApp myMonitorBuddy;

void setup() {
    cli();
    myMonitorBuddy.init();
}

void loop()
{
    myMonitorBuddy.update();
}