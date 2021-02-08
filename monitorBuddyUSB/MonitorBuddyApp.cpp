// 
// 
// 

#include "MonitorBuddyApp.h"

using BlinkCallback = void(*)(bool);
BlinkCallback blinkCallback = nullptr;

int LED = 13;
int LEDBlinkSpeed = 2;
unsigned long ledTime = 0;
bool ledOn = false;

char buttonState = 0;
char currentButtonState = 0;

extern "C"
{
    #include "v-usb/usbdrv/usbdrv.h"

    void monitorBuddyBlinkLED(bool rcv)
    {
        if (blinkCallback)
        {
            blinkCallback(rcv);
        }
    }
}

TimedPin MonitorBuddyApp::m_statusRcvLED(5, 20);
TimedPin MonitorBuddyApp::m_statusSndLED(4, 20);

MonitorBuddyApp::MonitorBuddyApp()
    : m_dial(9, 11)
    , m_buttonOne(7)
    , m_buttonTwo(6)
    , m_buttonDial(8)
{
}

void MonitorBuddyApp::init()
{
    usbDeviceDisconnect(); // enforce re-enumeration
    delay(500);
    
    usbDeviceConnect();
    usbInit();

    sei(); // Enable interrupts after re-enumeration

    Serial.print(F("setup complete. clock: ")); Serial.println(F_CPU);

    ledTime = millis();
    digitalWrite(LED, ledOn);

    usbSetInterrupt((uchar *)&m_interruptData, sizeof(m_interruptData));

    blinkCallback = [](bool rcv) {
        MonitorBuddyApp::BlinkStatus(rcv);
    };

    Serial.println("MonitorBuddyApp setup complete ...");
}

void MonitorBuddyApp::dialChanged(long newValue)
{
    Serial.print("DIAL CHANGE: "); Serial.println(newValue);
}

void MonitorBuddyApp::update()
{

    updateLED();

    usbPoll();

    m_dial.tick();

    // m_dialPosition = value_event(m_dial.getPosition(), [this](long v) {
    //    dialChanged(v);
    // });

    int position = m_dialPosition;

    // update usb tx/rx status
    m_statusRcvLED.update();
    m_statusSndLED.update();

    InterruptData newIntData;

    // pullup resistor mode, low means button pressed
    newIntData.m_buttonOne = digitalRead(m_buttonOne.m_pin) == LOW;
    newIntData.m_buttonTwo = digitalRead(m_buttonTwo.m_pin) == LOW;
    newIntData.m_buttonDial = digitalRead(m_buttonDial.m_pin) == LOW; 
    newIntData.m_dialRotation = m_dial.getPosition();

    // if (usbInterruptIsReady()) not neccessary afaict
    if (m_interruptData != newIntData)
    {
        byte packet[7];
        
        packet[0] = newIntData.m_buttonOne;
        packet[1] = newIntData.m_buttonTwo;
        packet[2] = newIntData.m_buttonDial;
        memcpy(packet+3, &newIntData.m_dialRotation, sizeof(long));

        usbSetInterrupt(packet, sizeof(packet));
        m_interruptData = newIntData;
        
        /*
        Serial.print("writing new inturrupt data, len: "); Serial.println(sizeof(m_interruptData));
        Serial.print("button1 state: "); Serial.println((byte)newIntData.m_buttonOne);
        Serial.print("button2 state: "); Serial.println((byte)newIntData.m_buttonTwo);
        Serial.print("buttonB state: "); Serial.println((byte)newIntData.m_buttonDial); 
        Serial.print("buttonD state[0]: "); Serial.println(packet[3]);
        Serial.print("buttonD state[1]: "); Serial.println(packet[4]);
        Serial.print("buttonD state[2]: "); Serial.println(packet[5]);
        Serial.print("buttonD state[3]: "); Serial.println(packet[6]);
        */
    }
}

void MonitorBuddyApp::updateLED()
{
    if (millis() - ledTime > (1000 / LEDBlinkSpeed))
    {
        ledOn = !ledOn;
        digitalWrite(LED, ledOn);
        ledTime = millis();
    }
}