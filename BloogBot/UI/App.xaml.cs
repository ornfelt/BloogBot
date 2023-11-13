﻿using System;
using System.Diagnostics;
using System.Windows;

namespace BloogBot.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            Debugger.Launch();
#endif

            WardenDisabler.Initialize();

            var mainWindow = new MainWindow();
            Current.MainWindow = mainWindow;
            mainWindow.Closed += (sender, args) => { Environment.Exit(0); };
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
