using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

/// <summary>
/// Namespace for the Combat Rogue Bot.
/// </summary>
namespace CombatRogueBot
{
    /// <summary>
    /// Determines if there are any additional targeting criteria for a given WoWUnit.
    /// </summary>
    /// <summary>
    /// Determines if there are any additional targeting criteria for a given WoWUnit.
    /// </summary>
    [Export(typeof(IBot))]

    class CombatRogueBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the combat rogue.
        /// </summary>
        public string Name => "Combat Rogue";

        /// <summary>
        /// Gets the file name of the CombatRogueBot.dll.
        /// </summary>
        public string FileName => "CombatRogueBot.dll";

        /// <summary>
        /// Determines if there are any additional targeting criteria for a given WoWUnit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> WoWUnit: Get Units
        /// ObjectManager -> Player: Get Player Level
        /// ObjectManager -> WoWUnit: Get Unit Level
        /// ObjectManager -> WoWUnit: Get Unit Reaction
        /// ObjectManager -> Player: Get Player Guid
        /// ObjectManager -> WoWUnit: Get Unit Guid
        /// ObjectManager -> WoWUnit: Get Unit Position
        /// ObjectManager -> Player: Get Player Position
        /// ObjectManager -> WoWUnit: Calculate Distance
        /// \enduml
        /// </remarks>
        bool AdditionalTargetingCriteria(WoWUnit u) =>
                            !ObjectManager.Units.Any(o =>
                                o.Level > ObjectManager.Player.Level - 4 &&
                                (o.UnitReaction == UnitReaction.Hated || o.UnitReaction == UnitReaction.Hostile) &&
                                o.Guid != ObjectManager.Player.Guid &&
                                o.Guid != u.Guid &&
                                o.Position.DistanceTo(u.Position) < 20
                            );

        /// <summary>
        /// Creates a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
                    new RestState(botStates, container);

        /// <summary>
        /// Creates a new instance of the MoveToTargetState class and returns it.
        /// </summary>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                    new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
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
        public void Test(IDependencyContainer container) { }
    }
}
