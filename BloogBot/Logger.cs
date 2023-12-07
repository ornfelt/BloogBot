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
        /// <remarks>
        /// \startuml
        /// BotSettings -> Bot: Initialize(BotSettings)
        /// Bot --> Bot: botSettings = parBotSettings
        /// \enduml
        /// </remarks>
        static public void Initialize(BotSettings parBotSettings)
        {
            botSettings = parBotSettings;
        }

        /// <summary>
        /// Logs the specified message to the console.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> Log: message
        /// Log -> Console: WriteLine(message)
        /// \enduml
        /// </remarks>
        static public void Log(object message) => Console.WriteLine(message);

        /// <summary>
        /// Logs a verbose message if verbose logging is enabled.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "LogVerbose Method" as L
        /// participant "Console" as Co
        /// C -> L: LogVerbose(message)
        /// alt botSettings.UseVerboseLogging is true
        ///     L -> Co: WriteLine(message)
        /// end
        /// \enduml
        /// </remarks>
        static public void LogVerbose(object message)
        {
            if (botSettings.UseVerboseLogging)
                Console.WriteLine(message);
        }
    }
}
