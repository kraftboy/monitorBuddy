// 
// 
// 

#include "MonitorBuddyApp.h"

using BlinkCallback = void(*)();
BlinkCallback blinkCallback = nullptr;

int LED = 13;
int LEDBlinkSpeed = 2;
unsigned long ledTime = 0;
bool ledOn = false;

struct AudioDeviceButton {
    unsigned long debounceStartTime = 0;
    unsigned long debounceIntervalMS = 500;
    int pin = PIND7;
} audioDeviceButton;

char buttonState = 0;
char currentButtonState = 0;

extern "C"
{
    #include "v-usb/usbdrv/usbdrv.h"

    void monitorBuddyBlinkLED()
    {
        if (blinkCallback)
        {
            blinkCallback();
        }
    }
}

TimedPin MonitorBuddyApp::m_statusLED(PIND6, 5);

void MonitorBuddyApp::init()
{
    usbInit();

    usbDeviceDisconnect(); // enforce re-enumeration
    delay(500);
    usbDeviceConnect();

    // enabled by default, I think
    // sei(); // Enable interrupts after re-enumeration

    Serial.print(F("setup complete. clock: ")); Serial.println(F_CPU);

    ledTime = millis();
    digitalWrite(LED, ledOn);

    // button
    pinMode(audioDeviceButton.pin, INPUT_PULLUP);
    usbSetInterrupt(&buttonState, 1);

    blinkCallback = []() {
        MonitorBuddyApp::BlinkStatus();
    };

    Serial.println("MonitorBuddyApp setup complete ...");
}

void MonitorBuddyApp::update()
{
    usbPoll();

    if (millis() - ledTime > (1000 / LEDBlinkSpeed))
    {
        ledOn = !ledOn;
        digitalWrite(LED, ledOn);
        ledTime = millis();
    }

    // update status
    m_statusLED.update();

    auto& ads = audioDeviceButton;
    auto state = digitalRead(ads.pin);
    buttonState = state == LOW;
    // if (usbInterruptIsReady()) not neccessary afaict
    {
        // pullup resistor mode, low is button pressed
        buttonState = state == LOW;
        if (currentButtonState != buttonState)
        {
            usbSetInterrupt(&buttonState, 1);
            currentButtonState = buttonState;
            Serial.print("button state: "); Serial.println(currentButtonState != 0);
        }
    }
}