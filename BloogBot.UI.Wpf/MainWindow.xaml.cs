// Ported from BloogBot/BloogBot/UI/MainWindow.xaml.cs (.NET Framework 4.8 -> .NET 9, WPF shell). Replica - do not redesign.
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BloogBot.UI.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // The original read the DataContext back out of the window, where
            // <Window.DataContext><local:MainViewModel /></Window.DataContext> had constructed it.
            // MainViewModel takes the shell's IThemeService now, so the shell constructs it here.
            DataContext = new MainViewModel(new WpfThemeService());
            var context = (MainViewModel)DataContext;
            context.InitializeObjectManager();

            // make sure the output window stays scrolled to the bottom
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += ((sender, e) =>
            {
                if (Console.VerticalOffset == Console.ScrollableHeight)
                    Console.ScrollToEnd();
            });
            timer.Start();

            Task.Run(() => BotService.Run(context));
        }
    }
}
