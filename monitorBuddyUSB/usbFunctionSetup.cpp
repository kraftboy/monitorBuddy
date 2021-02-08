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
    usbRequest_t* rq = (void*)data; // cast data to correct type
    return 0; // should not get here
}

void usbFunctionWriteOut(uchar* data, uchar len)
{
    // usbRxToken is the endpoint number, see usbdrv.h
    Serial.println("usbFunctionWriteOut"); Serial.println("usbFunctionWriteOut"); Serial.println(usbRxToken);
    
    if(usbRxToken == 1)
    { 
        data[0] = buttonState;
    }
}

