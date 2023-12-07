// Friday owns this file!

using BeastmasterHunterBot;
using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;

/// <summary>
/// Namespace for the Beast Master Hunter Bot.
/// </summary>
namespace BeastMasterHunterBot
{
    /// <summary>
    /// This class represents a Beast Master Hunter bot in World of Warcraft.
    /// </summary>
    /// <summary>
    /// This class represents a Beast Master Hunter bot in a World of Warcraft game.
    /// </summary>
    [Export(typeof(IBot))]
    class BeastMasterHunterBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Beast Master Hunter.
        /// </summary>
        public string Name => "Beast Master Hunter";

        /// <summary>
        /// Gets the file name of the BeastMasterHunterBot.dll.
        /// </summary>
        public string FileName => "BeastMasterHunterBot.dll";

        /// <summary>
        /// Determines if additional targeting criteria are met for a World of Warcraft unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "WoWUnit" as W
        /// C -> W: AdditionalTargetingCriteria(unit)
        /// note right: Returns true
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
        /// IBotState -> MoveToTargetState: CreateMoveToTargetState(botStates, container, target)
        /// \enduml
        /// </remarks>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                            new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// IBotState -> PowerlevelCombatState: CreatePowerlevelCombatState(botStates, container, target, powerlevelTarget)
        /// \enduml
        /// </remarks>
        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
                            new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        /// <summary>
        /// Gets the dependency container for the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetDependencyContainer Method" as GDC
        /// participant "DependencyContainer" as DC
        /// GDC -> DC: new DependencyContainer(AdditionalTargetingCriteria, CreateRestState, CreateMoveToTargetState, CreatePowerlevelCombatState, botSettings, probe, hotspots)
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
        /// Executes a test by calling the "StartAttack()" method on the player object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> Player: Get Player
        /// Player -> Player: LuaCall("StartAttack()")
        /// \enduml
        /// </remarks>
        public void Test(IDependencyContainer container)
        {
            var player = ObjectManager.Player;
            player.LuaCall("StartAttack()");
        }
    }
}
