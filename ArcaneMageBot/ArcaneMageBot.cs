using BloogBot;
using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;

/// <summary>
/// This namespace contains the implementation of the Arcane Mage bot.
/// </summary>
namespace ArcaneMageBot
{
    /// <summary>
    /// This class represents a bot for the Arcane Mage character in a game.
    /// </summary>
    [Export(typeof(IBot))]
    class ArcaneMageBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Arcane Mage.
        /// </summary>
        public string Name => "Arcane Mage";

        /// <summary>
        /// Gets the file name of the ArcaneMageBot.dll.
        /// </summary>
        public string FileName => "ArcaneMageBot.dll";

        /// <summary>
        /// Determines if additional targeting criteria are met for a World of Warcraft unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "AdditionalTargetingCriteria" as A
        /// C -> A: Call AdditionalTargetingCriteria(WoWUnit unit)
        /// A --> C: Return true
        /// \enduml
        /// </remarks>
        bool AdditionalTargetingCriteria(WoWUnit unit) => true;

        /// <summary>
        /// Creates a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreateRestState Method" as CRM
        /// participant "RestState" as RS
        /// CRM -> RS: new RestState(botStates, container)
        /// \enduml
        /// </remarks>
        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
                            new RestState(botStates, container);

        /// <summary>
        /// Creates a new instance of the MoveToTargetState class and returns it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreateMoveToTargetState()" as CMTS
        /// participant "MoveToTargetState" as MTS
        /// CMTS -> MTS: new MoveToTargetState(botStates, container, target)
        /// \enduml
        /// </remarks>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                            new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreatePowerlevelCombatState Method" as A
        /// participant "PowerlevelCombatState Object" as B
        /// A -> B: Create new instance
        /// note right: Parameters are botStates, container, target, powerlevelTarget
        /// \enduml
        /// </remarks>
        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
                            new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        /// <summary>
        /// Gets the dependency container for the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "BotSettings" as A
        /// participant "Probe" as B
        /// participant "Hotspot" as C
        /// participant "DependencyContainer" as D
        /// A -> D: botSettings
        /// B -> D: probe
        /// C -> D: hotspots
        /// \enduml
        /// </remarks>
        public IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots) =>
                            new DependencyContainer(
                                AdditionalTargetingCriteria,
                                CreateRestState,
                                CreateMoveToTargetState,
                                CreatePowerlevelCombatState,
                                botSettings,
                                probe,
                                hotspots);

        /// <summary>
        /// This method is used to test the functionality of the dependency container.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Test Method" as A
        /// participant "IDependencyContainer" as B
        /// A -> B: container
        /// \enduml
        /// </remarks>
        public void Test(IDependencyContainer container) { }
    }
}
