using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Enhancement Shaman Bot.
/// </summary>
namespace EnhancementShamanBot
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
        /// Updates the current state of the bot. If the target is tapped by another player or if there are aggressors targeting the target, the bot will remove all waiting actions, pop the current state from the stack, and return. Otherwise, it checks if the player is stuck. If the player is within a certain distance to the target, not casting a spell, the LightningBolt spell is ready, and the player has line of sight with the target, the bot will attempt to pull the target using the LightningBolt spell. If the player is moving, it will stop all movement. If a certain delay has passed, the bot will cast the LightningBolt spell and if successful, stop all movement, remove all waiting actions, pop the current state from the stack, and push a new CombatState onto the stack. If none of the above conditions are met, the bot will calculate the next waypoint using the Navigation class and move towards it.
        /// </summary>
        public void Update()
        {
            if (target.TappedByOther || (ObjectManager.Aggressors.Count() > 0 && !ObjectManager.Aggressors.Any(a => a.Guid == target.Guid)))
            {
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.Position.DistanceTo(target.Position) < 27 && !player.IsCasting && player.IsSpellReady(LightningBolt) && player.InLosWith(target.Position))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For("PullWithLightningBoltDelay", 100))
                {
                    if (!player.IsInCombat)
                        player.LuaCall($"CastSpellByName('{LightningBolt}')");

                    if (player.IsCasting || player.IsInCombat)
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
