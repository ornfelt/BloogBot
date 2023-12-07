using BloogBot;
using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;

/// <summary>
/// Namespace for the Protection Paladin Bot.
/// </summary>
namespace ProtectionPaladinBot
{
    /// <summary>
    /// Represents a bot for a Protection Paladin character in World of Warcraft.
    /// </summary>
    [Export(typeof(IBot))]
    class ProtectionPaladinBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Protection Paladin.
        /// </summary>
        public string Name => "Protection Paladin";

        /// <summary>
        /// Gets the file name of the ProtectionPaladinBot.dll.
        /// </summary>
        public string FileName => "ProtectionPaladinBot.dll";

        /// <summary>
        /// Determines if additional targeting criteria are met for a World of Warcraft unit.
        /// </summary>
        bool AdditionalTargetingCriteria(WoWUnit unit) => true;

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
        /// This method is used to test the functionality of the dependency container.
        /// </summary>
        public void Test(IDependencyContainer container) { }
    }
}
