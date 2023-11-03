using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the interface for a bot, which handles various AI functionalities.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents a bot interface.
    /// </summary>
    public interface IBot
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Retrieves the dependency container for the specified bot settings, probe, and hotspots.
        /// </summary>
        IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots);

        /// <summary>
        /// Checks if the process is currently running.
        /// </summary>
        bool Running();

        /// <summary>
        /// Starts the process with the specified dependency container and stop callback.
        /// </summary>
        void Start(IDependencyContainer container, Action stopCallback);

        /// <summary>
        /// Stops the execution of the program.
        /// </summary>
        void Stop();

        /// <summary>
        /// Triggers a travel action using the specified dependency container, with the option to reverse the travel path, and executes the provided callback afterwards.
        /// </summary>
        void Travel(IDependencyContainer container, bool reverseTravelPath, Action callback);

        /// <summary>
        /// Starts the power level with the specified dependency container and stop callback.
        /// </summary>
        void StartPowerlevel(IDependencyContainer container, Action stopCallback);

        /// <summary>
        /// This method is used to test the functionality of the dependency container.
        /// </summary>
        void Test(IDependencyContainer container);
    }
}
