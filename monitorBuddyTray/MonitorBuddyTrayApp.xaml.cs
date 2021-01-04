using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace monitorBuddyTray
{
    public partial class MonitorBuddyTrayApp : Window
    {
        private NotifyIcon trayIcon = null;
        private ContextMenu contextMenu = null;
        private USBMonitorBuddyDevice monitorBuddyDevice = null;
        private AudioMBDeviceManager monitorBuddyAudioDeviceManager = null;

        public MonitorBuddyTrayApp()
        {


            monitorBuddyDevice = new USBMonitorBuddyDevice();
            monitorBuddyAudioDeviceManager = new AudioMBDeviceManager();

            // for icon changes
            monitorBuddyDevice.USBConnectionChange += MonitorBuddyDevice_USBConnectionChange; ;

            // hook up the device change to the button from the usb device
            monitorBuddyDevice.ButtonPressed += monitorBuddyAudioDeviceManager.ChangeDevice;

            _ = monitorBuddyDevice.MonitorEndpoint();

            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.mbicon_disconnected
            };

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            trayIcon.MouseDown += TrayIcon_MouseDown;
            contextMenu = new ContextMenu();
            MenuItem exitItem = new MenuItem
            {
                Header = "Exit"
            };

            MenuItem deviceRotationItem = new MenuItem
            {
                Header = "Device Rotation"
            };

            foreach(var device in monitorBuddyAudioDeviceManager.AudioDevices)
            {
                var deviceMenuItem = new MenuItem
                {
                    Header = device.Name,
                    IsCheckable = true,
                    IsChecked = device.InRotation,
                    StaysOpenOnClick = true,
                    DataContext = device.Id
                };

                deviceMenuItem.Checked += DeviceMenuItem_Checked;
                deviceMenuItem.Unchecked += DeviceMenuItem_Unchecked;

                deviceRotationItem.Items.Add(deviceMenuItem);
            }

            var chimeOnChangeItem = new MenuItem
            {
                Header = "Notification Snd",
                IsCheckable = true,
                IsChecked = monitorBuddyAudioDeviceManager.PlayChimeOnChange,
            };

            chimeOnChangeItem.Checked += monitorBuddyAudioDeviceManager.ChimeOnChangeItem_Checked;
            chimeOnChangeItem.Unchecked += monitorBuddyAudioDeviceManager.ChimeOnChangeItem_Unchecked;

            deviceRotationItem.SubmenuClosed += DeviceRotationItem_SubmenuClosed;

            exitItem.Click += ExitApp;
            contextMenu.Items.Add(deviceRotationItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(chimeOnChangeItem);
            contextMenu.Items.Add(exitItem);
            
            // show tray icon
            trayIcon.Visible = true;

            App myApp = System.Windows.Application.Current as App;
            myApp.UnhandledException += Application_UnhandledException;
        }

        private void MonitorBuddyDevice_USBConnectionChange(object sender, EventArgs e)
        {
            var connectionState = e as USBMonitorBuddyDevice.USBConnectionEventArgs;
            if (connectionState.connected)
            {
                trayIcon.Icon = Properties.Resources.mbicon;
            }
            else
            {
                trayIcon.Icon = Properties.Resources.mbicon_disconnected;
            }
        }

        private void DeviceRotationItem_SubmenuClosed(object sender, RoutedEventArgs e)
        {
            contextMenu.IsOpen = false;
        }

        private void ExitCommon()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        private void Application_UnhandledException(object sender, EventArgs e)
        {
            ExitCommon();
        }

        private void DeviceMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            monitorBuddyAudioDeviceManager.SetInRotationState(menuItem.DataContext as string, true);
        }

        private void DeviceMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            monitorBuddyAudioDeviceManager.SetInRotationState(menuItem.DataContext as string, false);
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // throw new NotImplementedException();
        }

        private void TrayIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                contextMenu.IsOpen = true;
            }
        }

        public void ExitApp(object o, System.EventArgs args)
        {
            ExitCommon();

            System.Windows.Application.Current.Shutdown();
        }

    }
}
