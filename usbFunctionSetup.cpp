extern "C"
{
    #include "v-usb/usbdrv/usbdrv.h";
}

#include <Arduino.h>

#define USB_LED_FAST 0
#define USB_LED_SLOW  1

extern int LEDBlinkSpeed;
static int buttonState;

usbMsgLen_t usbFunctionSetup(uchar data[8])
{
    Serial.print("usbFunctionSetup called, endpoint: "); Serial.println(usbRxToken);

    usbRequest_t* rq = (void*)data; // cast data to correct type

    switch (rq->bRequest) { // custom command is in the bRequest field
    case USB_LED_FAST:
        LEDBlinkSpeed = 5;
        return 0;
    case USB_LED_SLOW:
        LEDBlinkSpeed = 2;
        return 0;
    }

    return 0; // should not get here
}

void usbFunctionWriteOut(uchar* data, uchar len)
{
    Serial.print("usbFunctionWriteOut called, enpoint: "); Serial.println(usbRxToken);

    // device set button
    if(usbRxToken == 1)
    { 
        data[0] = buttonState;
    }
}

