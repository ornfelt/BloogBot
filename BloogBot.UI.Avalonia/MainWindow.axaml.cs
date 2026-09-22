// Ported from BloogBot/BloogBot/UI/MainWindow.xaml.cs (.NET Framework 4.8 -> .NET 9, Avalonia shell). Replica - do not redesign.
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace BloogBot.UI.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // The original read the DataContext back out of the window, where
            // <Window.DataContext><local:MainViewModel /></Window.DataContext> had constructed it.
            // MainViewModel takes the shell's IThemeService now, so the shell constructs it here.
            DataContext = new MainViewModel(new AvaloniaThemeService());
            var context = (MainViewModel)DataContext;
            context.InitializeObjectManager();

            // make sure the output window stays scrolled to the bottom
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += ((sender, e) =>
            {
                // WPF's ScrollViewer.VerticalOffset and .ScrollableHeight are Offset.Y and
                // Extent.Height - Viewport.Height here; the comparison is the original's.
                if (Console.Offset.Y == Console.Extent.Height - Console.Viewport.Height)
                    Console.ScrollToEnd();
            });
            timer.Start();

            Task.Run(() => BotService.Run(context));
        }
    }
}
