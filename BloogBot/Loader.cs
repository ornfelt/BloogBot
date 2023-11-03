using System.Threading;
using BloogBot.UI;

/// <summary>
/// The BloogBot namespace contains classes for loading and running the bot.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a class that handles loading of data.
    /// </summary>
    /// <summary>
    /// Represents a class that handles loading of data.
    /// </summary>
    class Loader
    {
        /// <summary>
        /// Represents a static thread.
        /// </summary>
        static Thread thread;

        /// <summary>
        /// Loads the application in a new thread with the specified arguments.
        /// </summary>
        static int Load(string args)
        {
            thread = new Thread(App.Main);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return 1;
        }
    }
}
