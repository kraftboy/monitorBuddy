using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
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
        private MenuItem playDeviceRotationMenu;
        private MenuItem inputDeviceRotationMenu;

        private readonly string kRegistryAppName = "MonitorBuddy";

        public MonitorBuddyTrayApp()
        {


            monitorBuddyDevice = new USBMonitorBuddyDevice();
            monitorBuddyAudioDeviceManager = new AudioMBDeviceManager();

            // for icon changes
            monitorBuddyDevice.USBConnectionChange += MonitorBuddyDevice_USBConnectionChange; ;

            // hook up the device change to the button from the usb device, we can make this configurable later
            monitorBuddyDevice.ButtonOnePressed += monitorBuddyAudioDeviceManager.ChangePlayDevice;
            monitorBuddyDevice.ButtonTwoPressed += monitorBuddyAudioDeviceManager.ChangeRecordDevice;
            monitorBuddyDevice.DialChanged += monitorBuddyAudioDeviceManager.ChangeVolume;

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

            playDeviceRotationMenu = new MenuItem
            {
                Header = "Play Device Rotation"
            };

            inputDeviceRotationMenu = new MenuItem
            {
                Header = "Input Device Rotation"
            };

            monitorBuddyAudioDeviceManager.DevicesChanged += BuildDeviceMenus;
            monitorBuddyAudioDeviceManager.RefreshDevices();

            var chimeOnChangeItem = new MenuItem
            {
                Header = "Notification Sound",
                IsCheckable = true,
                IsChecked = monitorBuddyAudioDeviceManager.PlayChimeOnChange,
            };

            var runOnStartup = new MenuItem
            {
                Header = "Run on Startup",
                IsCheckable = true,
                IsChecked = IsStartupSet()
            };

            // set sound on change hanlders
            chimeOnChangeItem.Checked += monitorBuddyAudioDeviceManager.ChimeOnChangeItem_Checked;
            chimeOnChangeItem.Unchecked += monitorBuddyAudioDeviceManager.ChimeOnChangeItem_Checked;

            // set mb to startup handlers
            runOnStartup.Checked += RunOnStartup_Check;
            runOnStartup.Unchecked += RunOnStartup_Check;

            playDeviceRotationMenu.SubmenuClosed += DeviceRotationItem_SubmenuClosed;
            inputDeviceRotationMenu.SubmenuClosed += DeviceRotationItem_SubmenuClosed;

            exitItem.Click += ExitApp;
            contextMenu.Items.Add(playDeviceRotationMenu);
            contextMenu.Items.Add(inputDeviceRotationMenu);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(new MenuItem()
            {
                Header = "Preferences",
                Items =
                {
                    chimeOnChangeItem,
                    runOnStartup
                }
            });

            contextMenu.Items.Add(exitItem);
            
            // show tray icon
            trayIcon.Visible = true;

            App myApp = System.Windows.Application.Current as App;
            myApp.UnhandledException += Application_UnhandledException;
        }

        private void RunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BuildDeviceMenus(object source, EventArgs args)
        {
            playDeviceRotationMenu.Items.Clear();
            inputDeviceRotationMenu.Items.Clear();

            void BuildRotationMenu(MenuItem parent, List<AudioMBDeviceManager.AudioDeviceMBState> deviceList, bool playDevice)
            {
                foreach (var device in deviceList)
                {
                    var deviceMenuItem = new MenuItem
                    {
                        Header = device.Name,
                        IsCheckable = true,
                        IsChecked = device.InRotation,
                        StaysOpenOnClick = true,
                        DataContext = device.Id
                    };

                    deviceMenuItem.Checked += new RoutedEventHandler((s, e) => DeviceMenuItem_PlayDeviceRotation(s, e, playDevice));
                    deviceMenuItem.Unchecked += new RoutedEventHandler((s, e) => DeviceMenuItem_PlayDeviceRotation(s, e, playDevice));

                    parent.Items.Add(deviceMenuItem);
                }
            }

            BuildRotationMenu(playDeviceRotationMenu, monitorBuddyAudioDeviceManager.AudioPlayDevices, true);
            BuildRotationMenu(inputDeviceRotationMenu, monitorBuddyAudioDeviceManager.AudioInputDevices, false);
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

        private void DeviceMenuItem_PlayDeviceRotation(object sender, RoutedEventArgs e, bool playDevice)
        {
            var menuItem = sender as MenuItem;
            monitorBuddyAudioDeviceManager.SetInRotationState(playDevice, menuItem.DataContext as string, e.RoutedEvent == MenuItem.CheckedEvent);
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

        private void RunOnStartup_Check(object source, RoutedEventArgs args)
        {
            SetStartup(args.RoutedEvent == MenuItem.CheckedEvent);
        }

        private bool IsStartupSet()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return rk.GetValue(kRegistryAppName) != null;
        }

        private void SetStartup(bool enabled)
        {
            var exePath = Environment.GetCommandLineArgs()[0];
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enabled)
                rk.SetValue(kRegistryAppName, exePath);
            else
                rk.DeleteValue(kRegistryAppName, false);
        }
    }
}
