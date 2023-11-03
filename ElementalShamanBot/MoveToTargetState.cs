using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the Elemental Shaman Bot.
/// </summary>
namespace ElementalShamanBot
{
    /// <summary>
    /// Represents a class that handles moving to a target state.
    /// </summary>
    /// <summary>
    /// Represents a class that handles moving to a target state.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// Represents a constant string value for "Lightning Bolt".
        /// </summary>
        const string LightningBolt = "Lightning Bolt";

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
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;

        /// <summary>
        /// Initializes a new instance of the MoveToTargetState class.
        /// </summary>
        internal MoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        /// <summary>
        /// Updates the current state of the bot.
        /// If the target is tapped by another player or if there are aggressors targeting the target and the bot is not one of them,
        /// stops all movement, pops the current state from the stack, and returns.
        /// Checks if the bot is stuck.
        /// If the player is within 30 units of the target's position, not casting a spell, the LightningBolt spell is ready, and the player has line of sight with the target's position,
        /// stops all movement, pops the current state from the stack, pushes a new CombatState onto the stack, and returns.
        /// Gets the next waypoint for the player to move towards based on the current map, the player's position, and the target's position,
        /// and moves the player towards the next waypoint.
        /// </summary>
        public void Update()
        {
            if (target.TappedByOther || (ObjectManager.Aggressors.Count() > 0 && !ObjectManager.Aggressors.Any(a => a.Guid == target.Guid)))
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.Position.DistanceTo(target.Position) < 30 && !player.IsCasting && player.IsSpellReady(LightningBolt) && player.InLosWith(target.Position))
            {
                player.StopAllMovement();
                botStates.Pop();
                botStates.Push(new CombatState(botStates, container, target));
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
