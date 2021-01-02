using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace monitorBuddyTray
{
    class MonitorBuddyTrayApp
    {
        private NotifyIcon trayIcon = null;
        private ContextMenu contextMenu = null;
        private USBMonitorBuddyDevice monitorBuddyDevice = null;

        public MonitorBuddyTrayApp()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.TrayIcon
            };

            trayIcon.MouseDown += TrayIcon_MouseDown;
            contextMenu = new ContextMenu();
            MenuItem exitItem = new MenuItem
            {
                Header = "Exit"
            };

            exitItem.Click += ExitApp;
            contextMenu.Items.Add(exitItem);
            trayIcon.Visible = true;

            monitorBuddyDevice = new USBMonitorBuddyDevice();
            _ = monitorBuddyDevice.MonitorEndpoint();

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            throw new NotImplementedException();
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
            trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

    }
}
