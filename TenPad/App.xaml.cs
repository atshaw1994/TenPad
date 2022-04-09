using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TenPad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                MainWindow window = new();
                window.Show();
            }
            if (e.Args.Length == 1)
            {
                MainWindow window = new(e.Args[0]);
                window.Show();
            }
            
        }
    }
}
