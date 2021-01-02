// MonitorBuddy.h
#pragma once

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#include "TimedPin.h"

class MonitorBuddyApp
{
public:

	MonitorBuddyApp()
	{};

	void init(); // ie setup
	void update(); // ie loop

	static void BlinkStatus()
	{
		m_statusLED.fire();
	}

private:

	static TimedPin m_statusLED;
};

