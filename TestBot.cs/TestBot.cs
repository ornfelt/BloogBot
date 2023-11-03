using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// This namespace contains the implementation of the TestBot class, which is a bot that handles X.
/// </summary>
namespace TestBot
{
    /// <summary>
    /// Represents a test bot that handles specific functionality for testing purposes.
    /// </summary>
    [Export(typeof(IBot))]
    class TestBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the tester.
        /// </summary>
        public string Name => "Tester";

        /// <summary>
        /// Gets the file name of the TestBot.dll.
        /// </summary>
        public string FileName => "TestBot.dll";

        /// <summary>
        /// Determines if the specified WoWUnit meets additional targeting criteria.
        /// </summary>
        /// <param name="u">The WoWUnit to evaluate.</param>
        /// <returns>True if the WoWUnit meets the additional targeting criteria, otherwise false.</returns>
        bool AdditionalTargetingCriteria(WoWUnit u) => true;

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
        /// Executes a test on the specified dependency container.
        /// </summary>
        public void Test(IDependencyContainer container)
        {
            ThreadSynchronizer.RunOnMainThread(() =>
            {
                var player = ObjectManager.Player;

                var result = player.LuaCallWithResults("{0} = IsAttackAction(84)");
                if (result.Length > 0)
                {
                    Console.WriteLine(result.Length);
                    foreach (var r in result)
                    {
                        Console.WriteLine(r);
                    }
                }
            });
        }
    }
}

