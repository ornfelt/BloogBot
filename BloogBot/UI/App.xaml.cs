using System;
using System.Diagnostics;
using System.Windows;

/// <summary>
/// The BloogBot.UI namespace contains classes and components related to the user interface of the BloogBot application.
/// </summary>
namespace BloogBot.UI
{
    /// <summary>
    /// Represents the entry point for the application and handles the startup process.
    /// </summary>
    /// <summary>
    /// Represents the entry point for the application and handles the startup process.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Overrides the OnStartup method and initializes the application.
        /// </summary>
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
