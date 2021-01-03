using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace monitorBuddyTray
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        private static Mutex _mutex = new Mutex(true, "04C00614-2251-4F62-81C1-704013BE9C0D");

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                //app is already running! Exiting the application  
                Application.Current.Shutdown();
            }

            base.OnStartup(e);
        }
    }
}
