using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the implementation of the PowerlevelCombatState class, which represents a state in the RetributionPaladinBot.
/// </summary>
namespace RetributionPaladinBot
{
    /// <summary>
    /// This class manages the combat state for power-leveling in a bot game.
    /// </summary>
    class PowerlevelCombatState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;

        /// <summary>
        /// Initializes a new instance of the PowerlevelCombatState class.
        /// </summary>
        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        public void Update()
        {
            // TODO
        }
    }
}
