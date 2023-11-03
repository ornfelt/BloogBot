using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

/// <summary>
/// This namespace contains the implementation of the Balance Druid bot, which handles the behavior of a Balance Druid character in the game.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents a bot for playing as a Balance Druid.
    /// </summary>
    /// <summary>
    /// Represents a bot for playing as a Balance Druid.
    /// </summary>
    // NOTES:
    //  - Make sure you put 2/2 points into the Nature's Reach talent as soon as possible. This profile assumes you'll have the increased range.
    [Export(typeof(IBot))]
    class BalanceDruidBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Balance Druid.
        /// </summary>
        public string Name => "Balance Druid";

        /// <summary>
        /// Gets the file name of the BalanceDruidBot.dll.
        /// </summary>
        public string FileName => "BalanceDruidBot.dll";

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
        /// This method is used to test the functionality of the IDependencyContainer.
        /// </summary>
        public void Test(IDependencyContainer container)
        {
        }
    }
}
