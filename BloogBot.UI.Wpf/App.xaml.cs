// Ported from BloogBot/BloogBot/UI/App.xaml.cs (.NET Framework 4.8 -> .NET 9, WPF shell). Replica - do not redesign.
using System;
using System.Diagnostics;
using System.Windows;

namespace BloogBot.UI.Wpf
{
    public partial class App : Application
    {
        // Added by the .NET 9 port. The original was an ApplicationDefinition in a WinExe, so the
        // XAML compiler generated Main for it. BloogBot is a library now - hostfxr loads it and
        // BloogBot/Loader.cs starts this method on an STA thread - so the shell provides its own.
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

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
