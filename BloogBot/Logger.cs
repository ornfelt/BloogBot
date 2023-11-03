using System;

/// <summary>
/// This class handles logging functionality for the BloogBot namespace.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a logger class that handles logging messages to the console.
    /// </summary>
    /// <summary>
    /// Represents a logger class that handles logging messages to the console.
    /// </summary>
    static public class Logger
    {
        /// <summary>
        /// Represents the settings for the bot.
        /// </summary>
        static BotSettings botSettings;

        /// <summary>
        /// Initializes the bot with the specified settings.
        /// </summary>
        static public void Initialize(BotSettings parBotSettings)
        {
            botSettings = parBotSettings;
        }

        /// <summary>
        /// Logs the specified message to the console.
        /// </summary>
        static public void Log(object message) => Console.WriteLine(message);

        /// <summary>
        /// Logs a verbose message if verbose logging is enabled.
        /// </summary>
        static public void LogVerbose(object message)
        {
            if (botSettings.UseVerboseLogging)
                Console.WriteLine(message);
        }
    }
}
