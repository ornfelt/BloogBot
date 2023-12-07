using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;

/// <summary>
/// Namespace for the Enhancement Shaman Bot.
/// </summary>
namespace EnhancementShamanBot
{
    /// <summary>
    /// This class represents a bot for the Enhancement Shaman character in World of Warcraft.
    /// </summary>
    [Export(typeof(IBot))]
    class EnhancementShamanBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Enhancement Shaman.
        /// </summary>
        public string Name => "Enhancement Shaman";

        /// <summary>
        /// Gets the file name of the EnhancementShamanBot.dll.
        /// </summary>
        public string FileName => "EnhancementShamanBot.dll";

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
        /// Executes the Test method on the main thread, causing the player to jump.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Test -> ThreadSynchronizer: RunOnMainThread
        /// ThreadSynchronizer -> ObjectManager: Player.Jump
        /// \enduml
        /// </remarks>
        public void Test(IDependencyContainer container)
        {
            ThreadSynchronizer.RunOnMainThread(() =>
            {
                ObjectManager.Player.Jump();
            });
        }
    }
}
