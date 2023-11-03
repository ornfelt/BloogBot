using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Frost Mage bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents a state where the bot moves towards its target.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot moves towards its target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// The constant string representing the wait key for the Frost Mage pull.
        /// </summary>
        const string waitKey = "FrostMagePull";

        /// <summary>
        /// Represents a constant string value for "Fireball".
        /// </summary>
        const string Fireball = "Fireball";
        /// <summary>
        /// The constant string representing the spell "Frostbolt".
        /// </summary>
        const string Frostbolt = "Frostbolt";

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
        /// Gets the pulling spell.
        /// </summary>
        readonly string pullingSpell;
        /// <summary>
        /// Represents a readonly integer range.
        /// </summary>
        readonly int range;

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

            if (player.KnowsSpell(Frostbolt))
                pullingSpell = Frostbolt;
            else
                pullingSpell = Fireball;

            range = 28 + (ObjectManager.GetTalentRank(3, 11) * 3);
        }

        /// <summary>
        /// Updates the current state of the bot. If the player is currently casting a spell, the method returns without performing any actions. 
        /// If the target is tapped by another player or if the closest target has a different GUID than the current target, the player stops all movement and pops the current state from the stack. 
        /// The method also checks if the player is stuck and takes appropriate action. 
        /// If the player is within range of the target and has line of sight, the method stops all movement and waits for a specified amount of time. 
        /// After the wait time has passed, the method stops all movement, removes the wait key, and if the player is not in combat, casts a specified spell. 
        /// The method then pops the current state from the stack and pushes a new CombatState onto the stack. 
        /// If the player is not within range of the target or does not have line of sight, the method calculates the next waypoint and moves the player towards it.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting)
                return;

            if (target.TappedByOther || container.FindClosestTarget()?.Guid != target.Guid)
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget <= range && player.InLosWith(target.Position))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For(waitKey, 250))
                {
                    player.StopAllMovement();
                    Wait.Remove(waitKey);

                    if (!player.IsInCombat)
                        player.LuaCall($"CastSpellByName('{pullingSpell}')");

                    botStates.Pop();
                    botStates.Push(new CombatState(botStates, container, target));
                    return;
                }
            }
            else
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
        }
    }
}
