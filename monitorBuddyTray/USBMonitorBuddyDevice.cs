using System;
using System.Linq;
using System.Threading.Tasks;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace monitorBuddyTray
{
    class USBMonitorBuddyDevice : IDisposable
    {
        UsbEndpointReader endpointReader = null;
        IUsbDevice device = null;
        IUsbDevice monitorBuddyDevice = null;
        byte[] endpointBuffer = new byte[1];
        int endpointBufferSize = 1;

        static readonly string mfrName = "sirsonic.com";
        static readonly int interfaceID = 0;

        private bool monitoring = true;
        

        public USBMonitorBuddyDevice()
        {
            
        }

        public bool GetDevice(UsbContext context)
        {
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

            return false;
        }

        public async Task MonitorEndpoint()
        {
            await Task.Run(() =>
            {
                using (var context = new UsbContext())
                {
                    byte[] endpointBufferTmp = new byte[1];
                    while (monitoring)
                    {
                        try
                        {
                            if (device is null || !device.IsOpen)
                            {
                                if (!GetDevice(context))
                                    continue;
                            }

                            int bytesRead = 0;
                            var result = endpointReader.Read(endpointBuffer, 0, out bytesRead);
                            if (result == LibUsbDotNet.Error.Success)
                            {
                                if (endpointBuffer[0] != endpointBufferTmp[0])
                                {
                                    endpointBuffer[0] = endpointBufferTmp[0];
                                    Console.WriteLine($"Value changed: {endpointBufferTmp[0]}");

                                    try
                                    {
                                        ToastContent toastContent = new ToastContentBuilder()
                                            .AddText("Device changed!")
                                            .AddAudio(new ToastAudio()
                                            {
                                                Silent = true
                                            })
                                            .GetToastContent();

                                        // And create the toast notification
                                        var toast = new ToastNotification(toastContent.GetXml())
                                        {
                                            Tag = "ChangedDevice",
                                            ExpirationTime = DateTime.Now.AddSeconds(10)
                                        };

                                        // And then show it
                                        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine($"Exception {e}");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            device.Close();
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            if(monitorBuddyDevice != null)
            {
                monitorBuddyDevice.ReleaseInterface(interfaceID);
                monitorBuddyDevice.Close();
            }
        }
    }
}
