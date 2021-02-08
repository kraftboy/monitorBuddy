using System;
using System.Linq;
using System.Threading.Tasks;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using LibUsbDotNet;
using Windows.Management;

using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Management;
using System.Runtime.InteropServices;

namespace monitorBuddyTray
{
    class USBMonitorBuddyDevice : IDisposable
    {

        public enum EButton
        {
            ButtonOne,
            ButtonTwo,
            DialButton
        };

        public class ButtonEventArgs : EventArgs
        {
            public EButton? buttonPressed = null;
        };

        public class DialRotationEventArgs : EventArgs
        {
            public long dialChange = 0;
        };

        UsbEndpointReader endpointReader = null;

        IUsbDevice device = null;
        IUsbDevice monitorBuddyDevice = null;
        
        static readonly int endpointBufferSize = 7;
        byte[] endpointBuffer = new byte[endpointBufferSize];

        static readonly string mfrName = "sirsonic.com";
        static readonly int interfaceID = 0;

        private bool monitoring = true;
        private bool connectionState = false;

        public USBMonitorBuddyDevice()
        {
            MonitorUSBEvents();
        }

        public async Task<bool> GetDevice(UsbContext context)
        {
            Console.WriteLine("Looking for device ...");
            // Dump all devices and descriptor information to console output.
            foreach (var usbDevice in context.List())
            {
                if (usbDevice.TryOpen())
                {
                    if (usbDevice.Info.Manufacturer == mfrName)
                    {
                        device = usbDevice;
                        if (usbDevice.ClaimInterface(interfaceID))
                        {
                            monitorBuddyDevice = usbDevice;

                            // Console.WriteLine($"Device open, interface 0 claimed: {device.ToString()}");
                            endpointReader = monitorBuddyDevice.OpenEndpointReader(ReadEndpointID.Ep01, endpointBufferSize, EndpointType.Interrupt);

                            return true;
                        }
                    }
                }
            }

            await Task.Delay(2000);

            return false;
        }

        public async Task MonitorEndpoint()
        {
            await Task.Run(async () =>
            {
                using (var context = new UsbContext())
                {
                    byte[] endpointBufferTmp = new byte[endpointBufferSize];
                    while (monitoring)
                    {
                        try
                        {
                            if (device is null || !device.IsOpen)
                            {
                                bool deviceFound = await GetDevice(context);
                                if (deviceFound)
                                {
                                    OnUSBConnectedEvent(true);
                                }
                                else
                                {
                                    OnUSBConnectedEvent(false);
                                    continue;
                                }
                            }

                            int bytesRead = 0;
                            var result = endpointReader.Read(endpointBufferTmp, 100, out bytesRead);
                            if (result == LibUsbDotNet.Error.Success)
                            {
                                if (!endpointBufferTmp.SequenceEqual(endpointBuffer))
                                {
                                    // todo make this button push check more legible/general
                                    if (endpointBufferTmp[0] != endpointBuffer[0] && endpointBufferTmp[0] != 0)
                                    {
                                        OnButtonPressed(ButtonOnePressed);
                                    }
                                    if (endpointBufferTmp[1] != endpointBuffer[1] && endpointBufferTmp[1] != 0)
                                    {
                                        OnButtonPressed(ButtonTwoPressed);
                                    }
                                    if (endpointBufferTmp[2] != endpointBuffer[2] && endpointBufferTmp[2] != 0)
                                    {
                                        OnButtonPressed(DialButtonPressed);
                                    }
                                    if (endpointBufferTmp[3] != endpointBuffer[3])
                                    {
                                        var currentDialValue = BitConverter.ToInt32(endpointBuffer, 3);
                                        var newDialValue = BitConverter.ToInt32(endpointBufferTmp, 3);
                                        var dialDiff = newDialValue - currentDialValue;
                                        OnDialChanged(new DialRotationEventArgs() { dialChange = dialDiff });
                                    }

                                    // save state
                                    endpointBufferTmp.CopyTo(endpointBuffer, 0);
                                }
                            }
                            
                        }
                        catch (Exception e)
                        {
                            OnUSBConnectedEvent(false);
                            if (device != null)
                                device.Close();
                        }
                    }
                }
            });
        }


        public event EventHandler ButtonOnePressed;
        public event EventHandler ButtonTwoPressed;
        public event EventHandler DialButtonPressed;

        protected virtual void OnButtonPressed(EventHandler handler)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler DialChanged;
        protected virtual void OnDialChanged(DialRotationEventArgs e)
        {
            EventHandler handler = DialChanged;
            handler?.Invoke(this, e);
        }

        public class USBConnectionEventArgs : EventArgs
        {
            public bool connected = false;
        }

        public event EventHandler USBConnectionChange;
        private void OnUSBConnectedEvent(bool connected)
        {
            if (connectionState != connected)
            {
                connectionState = connected;
                USBConnectionChange?.Invoke(this, new USBConnectionEventArgs() { connected = connected }); ;
            }
        }


        public void Dispose()
        {
            if (monitorBuddyDevice != null)
            {
                monitorBuddyDevice.ReleaseInterface(interfaceID);
                monitorBuddyDevice.Close();
            }
        }

        // libusb doesn't support hotplug in windows, so using something from
        // https://stackoverflow.com/questions/620144/detecting-usb-drive-insertion-and-removal-using-windows-service-and-c-sharp
        // basically Win32_DeviceChangeEvent is the system detecting that *something* has been added/removed, but doesn't tell us
        // which device. We can check with __instanceAdded/removed events __InstanceCreationEvent/__InstanceDeletionEvent events,
        // but they're polling, with expiration windows, and imply cpu load
        // so we just clear our the usb device when any device is removed just to force a re-poll for our specific device
        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            device = null;
        }

        private ManagementEventWatcher insertWatcher;
        public void MonitorUSBEvents()
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

            insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();
        }
    }
}
