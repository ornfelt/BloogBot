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
        /// <remarks>
        /// \startuml
        /// participant "BotSettings" as A
        /// participant "Probe" as B
        /// participant "Hotspot" as C
        /// participant "IDependencyContainer" as D
        /// A -> D: GetDependencyContainer
        /// B -> D: GetDependencyContainer
        /// C -> D: GetDependencyContainer
        /// \enduml
        /// </remarks>
        IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots);

        /// <summary>
        /// Checks if the process is currently running.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : Running()
        /// \enduml
        /// </remarks>
        bool Running();

        /// <summary>
        /// Starts the process with the specified dependency container and stop callback.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as A
        /// participant "IDependencyContainer" as B
        /// A -> B: Start(container, stopCallback)
        /// \enduml
        /// </remarks>
        void Start(IDependencyContainer container, Action stopCallback);

        /// <summary>
        /// Stops the execution of the program.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : Stop()
        /// \enduml
        /// </remarks>
        void Stop();

        /// <summary>
        /// Triggers a travel action using the specified dependency container, with the option to reverse the travel path, and executes the provided callback afterwards.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Function Caller" as FC
        /// participant "Travel Function" as TF
        /// FC -> TF: Travel(container, reverseTravelPath, callback)
        /// \enduml
        /// </remarks>
        void Travel(IDependencyContainer container, bool reverseTravelPath, Action callback);

        /// <summary>
        /// Starts the power level with the specified dependency container and stop callback.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Function Caller" as A
        /// participant "StartPowerlevel Function" as B
        /// A -> B: StartPowerlevel(container, stopCallback)
        /// \enduml
        /// </remarks>
        void StartPowerlevel(IDependencyContainer container, Action stopCallback);

        /// <summary>
        /// This method is used to test the functionality of the dependency container.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "Test Function" as T
        /// C -> T: Test(container)
        /// \enduml
        /// </remarks>
        void Test(IDependencyContainer container);
    }
}
