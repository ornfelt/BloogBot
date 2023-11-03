using System;
using System.Windows;
using System.Windows.Threading;

/// <summary>
/// The UI namespace contains classes and components related to the user interface of the application.
/// </summary>
namespace BloogBot.UI
{
    /// <summary>
    /// Represents the main window of the application.
    /// </summary>
    /// <summary>
    /// Represents the main window of the application.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
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
        }
    }
}
