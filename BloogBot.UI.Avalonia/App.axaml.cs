// Ported from BloogBot/BloogBot/UI/App.xaml.cs (.NET Framework 4.8 -> .NET 9, Avalonia shell). Replica - do not redesign.
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace BloogBot.UI.Avalonia
{
    public partial class App : Application
    {
        // Added by the .NET 9 port. The original was an ApplicationDefinition in a WinExe, so the
        // XAML compiler generated Main for it. BloogBot is a library now - hostfxr loads it and
        // BloogBot/Loader.cs starts this method on an STA thread - so the shell provides its own.
        // The WPF shell's equivalent is App.Main in BloogBot.UI.Wpf; both carry the same name and
        // the same signature, which is what lets Loader.cs bind either one by reflection.
        [STAThread]
        public static void Main()
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .StartWithClassicDesktopLifetime(new string[0]);
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Avalonia's counterpart to WPF's Application.OnStartup. The body is the original's, with
        // the desktop lifetime standing in for Application.Current.MainWindow.
        public override void OnFrameworkInitializationCompleted()
        {
#if DEBUG
            Debugger.Launch();
#endif

            WardenDisabler.Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
                mainWindow.Closed += (sender, args) => { Environment.Exit(0); };
                mainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
