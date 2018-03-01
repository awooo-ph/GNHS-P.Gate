using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Properties;
using System.Linq;
using System.Threading;
using System.Windows;
using GNHSP.Gate.Properties;
using GNHSP.Gate.ViewModels;

namespace GNHSP.Gate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            awooo.IsRunning = true;
            awooo.Context = SynchronizationContext.Current;
            SMS.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.Save();
            SmsSettings.Default.Save();
            Keyboard.UnHook();
            SMS.Stop();
            base.OnExit(e);
        }
    }
}
