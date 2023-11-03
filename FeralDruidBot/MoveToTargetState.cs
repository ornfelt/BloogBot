using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the FeralDruidBot.
/// </summary>
namespace FeralDruidBot
{
    /// <summary>
    /// Represents a state where the bot moves towards a target.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot moves towards a target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Wrath".
        /// </summary>
        const string Wrath = "Wrath";

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
        /// stops all movement, clears all waiting tasks, pops the current state from the state stack, and returns.
        /// Checks if the bot is stuck using the stuckHelper.
        /// If the player is within 27 units of the target, not casting a spell, the Wrath spell is ready, and the player has line of sight with the target,
        /// stops all movement if the player is moving, waits for a delay named "PullWithWrathDelay",
        /// casts the Wrath spell if the player is not in combat, stops all movement, clears all waiting tasks, pops the current state from the state stack,
        /// and pushes a new CombatState onto the state stack.
        /// Gets the next waypoint using the Navigation class based on the current map, player position, and target position,
        /// and moves the player toward the next waypoint.
        /// </summary>
        public void Update()
        {
            if (target.TappedByOther || (ObjectManager.Aggressors.Count() > 0 && !ObjectManager.Aggressors.Any(a => a.Guid == target.Guid)))
            {
                player.StopAllMovement();
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.Position.DistanceTo(target.Position) < 27 && !player.IsCasting && player.IsSpellReady(Wrath) && player.InLosWith(target.Position))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For("PullWithWrathDelay", 100))
                {
                    if (!player.IsInCombat)
                        player.LuaCall($"CastSpellByName('{Wrath}')");

                    if (player.IsCasting || player.CurrentShapeshiftForm != "Human Form" || player.IsInCombat)
                    {
                        player.StopAllMovement();
                        Wait.RemoveAll();
                        botStates.Pop();
                        botStates.Push(new CombatState(botStates, container, target));
                    }
                }
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
