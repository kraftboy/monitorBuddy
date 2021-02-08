// MonitorBuddy.h
#pragma once
#include "ValueActor.h"

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#include "mytypes.h"
#include "TimedPin.h"
#include <RotaryEncoder.h>

class MonitorBuddyApp
{
public:

	MonitorBuddyApp();

	void init(); // ie setup
	void update(); // ie loop

	static void BlinkStatus(bool rcv)
	{
		auto& statusLED = rcv ? m_statusRcvLED : m_statusSndLED;
		statusLED.fire();
	}

private:

	// one button
	struct AudioDeviceButton
	{
		AudioDeviceButton(int pin) : m_pin(pin)
		{
			pinMode(m_pin, INPUT_PULLUP);
		}

		unsigned long m_debounceStartTime = 0;
		unsigned long m_debounceIntervalMS = 500;
		int m_pin = NOT_A_PIN;
	};

	// state of the data presented to USB interrupt
	struct InterruptData
	{
		uchar m_buttonOne;
		uchar m_buttonTwo;
		uchar m_buttonDial; 
		long m_dialRotation;
		
		bool operator!=(InterruptData const& other)
		{
			return memcmp(this, &other, sizeof(*this)) != 0;
		}
	};

	void dialChanged(long newValue);
	void updateLED();

	static TimedPin m_statusRcvLED;
	static TimedPin m_statusSndLED;

	AudioDeviceButton m_buttonOne;
	AudioDeviceButton m_buttonTwo;
	AudioDeviceButton m_buttonDial;

	InterruptData m_interruptData;

	RotaryEncoder m_dial;
	ValueActor<long> m_dialPosition = 0;
};

