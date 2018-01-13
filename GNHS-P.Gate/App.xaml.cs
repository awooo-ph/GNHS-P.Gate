﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Keyboard.UnHook();
        }
    }
}
