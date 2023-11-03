using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the Frost Mage bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Determines if additional targeting criteria are met for a World of Warcraft unit.
    /// </summary>
    /// <summary>
    /// Determines if additional targeting criteria are met for a World of Warcraft unit.
    /// </summary>
    [Export(typeof(IBot))]
    class FrostMageBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Frost Mage.
        /// </summary>
        public string Name => "Frost Mage";

        /// <summary>
        /// Gets the file name of the FrostMageBot.dll.
        /// </summary>
        public string FileName => "FrostMageBot.dll";

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
        /// This method tests the functionality of the IDependencyContainer by retrieving the target object from the ObjectManager and printing its pointer value if it exists.
        /// </summary>
        public void Test(IDependencyContainer container)
        {
            var target = ObjectManager.Units.FirstOrDefault(u => u.Guid == ObjectManager.Player.TargetGuid);

            if (target != null)
            {
                Console.WriteLine(ObjectManager.Player.Pointer.ToString("X"));
            }
        }
    }
}
