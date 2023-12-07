using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Balance Druid Bot.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// This class defines the state of a bot when it is moving to a target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Wrath".
        /// </summary>
        const string Wrath = "Wrath";
        /// <summary>
        /// Represents the constant string "Starfire".
        /// </summary>
        const string Starfire = "Starfire";
        /// <summary>
        /// Represents the constant string "Moonkin Form".
        /// </summary>
        const string MoonkinForm = "Moonkin Form";

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
        /// Represents a readonly integer range.
        /// </summary>
        readonly int range;
        /// <summary>
        /// Gets the pulling spell.
        /// </summary>
        readonly string pullingSpell;

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

            if (player.Level <= 19)
                range = 28;
            else if (player.Level == 20)
                range = 31;
            else
                range = 34;

            if (player.KnowsSpell(Starfire))
                pullingSpell = Starfire;
            else
                pullingSpell = Wrath;
        }

        /// <summary>
        /// Updates the current state of the bot. If the target is tapped by another player or if there are aggressors targeting the target, the bot will remove all waiting actions and pop the current state. If the player is casting a spell, the method will return. If the player knows the MoonkinForm spell but does not have the MoonkinForm buff, the method will cast the MoonkinForm spell. If the player is within range of the target and not casting a spell, and the pulling spell is ready and the player has line of sight with the target, the method will stop all movement, wait for a delay, and then cast the pulling spell. If the player is casting a spell or in combat after casting the pulling spell, the method will stop all movement, remove all waiting actions, pop the current state, and push a new CombatState. Finally, the method will calculate the next waypoint and move the player towards it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> target: Check TappedByOther
        /// Update -> ObjectManager: Check Aggressors
        /// alt target.TappedByOther is true or Aggressors exist
        ///     Update -> Wait: RemoveAll
        ///     Update -> botStates: Pop
        ///     Update -> return
        /// else target.TappedByOther is false and Aggressors do not exist
        ///     Update -> stuckHelper: CheckIfStuck
        ///     alt player.IsCasting is true
        ///         Update -> return
        ///     else player.IsCasting is false
        ///         Update -> player: Check KnowsSpell(MoonkinForm) and HasBuff(MoonkinForm)
        ///         alt player knows MoonkinForm and does not have MoonkinForm buff
        ///             Update -> player: LuaCall(CastSpellByName(MoonkinForm))
        ///         end
        ///         Update -> player: Check Position.DistanceTo(target.Position), IsCasting, IsSpellReady(pullingSpell), InLosWith(target.Position)
        ///         alt player is in range, not casting, spell is ready, and in line of sight with target
        ///             Update -> player: Check IsMoving
        ///             alt player is moving
        ///                 Update -> player: StopAllMovement
        ///             end
        ///             Update -> Wait: For("BalanceDruidPullDelay", 100)
        ///             alt Wait is true
        ///                 Update -> player: Check IsInCombat
        ///                 alt player is not in combat
        ///                     Update -> player: LuaCall(CastSpellByName(pullingSpell))
        ///                 end
        ///                 Update -> player: Check IsCasting or IsInCombat
        ///                 alt player is casting or in combat
        ///                     Update -> player: StopAllMovement
        ///                     Update -> Wait: RemoveAll
        ///                     Update -> botStates: Pop
        ///                     Update -> botStates: Push(new CombatState(botStates, container, target))
        ///                 end
        ///             end
        ///             Update -> return
        ///         else player is not in range, is casting, spell is not ready, or not in line of sight with target
        ///             Update -> Navigation: GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false)
        ///             Update -> player: MoveToward(nextWaypoint)
        ///         end
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (target.TappedByOther || (ObjectManager.Aggressors.Count() > 0 && !ObjectManager.Aggressors.Any(a => a.Guid == target.Guid)))
            {
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.IsCasting)
                return;

            if (player.KnowsSpell(MoonkinForm) && !player.HasBuff(MoonkinForm))
            {
                player.LuaCall($"CastSpellByName('{MoonkinForm}')");
            }

            if (player.Position.DistanceTo(target.Position) < range && !player.IsCasting && player.IsSpellReady(pullingSpell) && player.InLosWith(target.Position))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For("BalanceDruidPullDelay", 100))
                {
                    if (!player.IsInCombat)
                        player.LuaCall($"CastSpellByName('{pullingSpell}')");

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
