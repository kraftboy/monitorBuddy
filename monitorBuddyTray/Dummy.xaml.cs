using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace monitorBuddyTray
{
    /// <summary>
    /// Interaction logic for Dummy.xaml
    /// </summary>
    public partial class Dummy : Window
    {
        static private MonitorBuddyTrayApp myApp;

        public Dummy()
        {
            InitializeComponent();

            if(StartUp.RunStartup())
            {
                myApp = new MonitorBuddyTrayApp();
            }
        }
    }
}
