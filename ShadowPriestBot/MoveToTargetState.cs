using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// Namespace for the ShadowPriestBot, which handles the movement and actions of the bot.
/// </summary>
namespace ShadowPriestBot
{
    /// <summary>
    /// Represents a state where the bot moves towards its target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Holy Fire".
        /// </summary>
        const string HolyFire = "Holy Fire";
        /// <summary>
        /// Represents the constant string "Mind Blast".
        /// </summary>
        const string MindBlast = "Mind Blast";
        /// <summary>
        /// Represents the constant string "Power Word: Shield".
        /// </summary>
        const string PowerWordShield = "Power Word: Shield";
        /// <summary>
        /// The constant string representing "Shadowform".
        /// </summary>
        const string ShadowForm = "Shadowform";
        /// <summary>
        /// Represents the constant string "Smite".
        /// </summary>
        const string Smite = "Smite";
        /// <summary>
        /// Represents the constant string "WeakenedSoul".
        /// </summary>
        const string WeakenedSoul = "WeakenedSoul";

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
        /// Initializes a new instance of the MoveToTargetState class.
        /// </summary>
        internal MoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);

            if (player.HasBuff(ShadowForm))
                pullingSpell = MindBlast;
            else if (player.KnowsSpell(HolyFire))
                pullingSpell = HolyFire;
            else
                pullingSpell = Smite;
        }

        /// <summary>
        /// Updates the current state of the bot. If the target is tapped by another player or if the closest target has a different GUID than the current target, stops all movement and pops the current state from the stack.
        /// If the distance to the target is less than 27, stops movement if the player is moving. If the player is not casting and the pulling spell is ready, checks if the player knows the Power Word Shield spell and if the player has the Power Word Shield buff or is in combat. If the conditions are met, sets the target, removes the delay, casts the pulling spell, stops all movement, pops the current state from the stack, and pushes a new CombatState onto the stack.
        /// If the player knows the Power Word Shield spell and does not have the Weakened Soul debuff or the Power Word Shield buff, casts the Power Word Shield spell.
        /// If the distance to the target is greater than or equal to 27, checks if the player is stuck using the stuckHelper. Gets the next waypoint using the Navigation class and moves toward it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Target: Check TappedByOther or FindClosestTarget
        /// Target --> Update: Return Guid
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> Player: Get Position
        /// Player --> Update: Return Position
        /// Update -> Target: Get Position
        /// Target --> Update: Return Position
        /// Update -> Player: Check IsMoving
        /// Player --> Update: Return IsMoving
        /// Update -> Player: StopAllMovement
        /// Update -> Player: Check IsCasting and IsSpellReady
        /// Player --> Update: Return Status
        /// Update -> Player: Check KnowsSpell, HasBuff, IsInCombat
        /// Player --> Update: Return Status
        /// Update -> Wait: For "ShadowPriestPullDelay"
        /// Wait --> Update: Return Status
        /// Update -> Player: SetTarget
        /// Update -> Wait: Remove "ShadowPriestPullDelay"
        /// Update -> Player: Check IsInCombat
        /// Player --> Update: Return IsInCombat
        /// Update -> Player: LuaCall
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> BotStates: Push new CombatState
        /// Update -> Player: Check KnowsSpell, HasDebuff, HasBuff
        /// Player --> Update: Return Status
        /// Update -> Player: LuaCall
        /// Update -> StuckHelper: CheckIfStuck
        /// Update -> Navigation: GetNextWaypoint
        /// Navigation --> Update: Return nextWaypoint
        /// Update -> Player: MoveToward nextWaypoint
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (target.TappedByOther || container.FindClosestTarget()?.Guid != target.Guid)
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 27)
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (!player.IsCasting && player.IsSpellReady(pullingSpell))
                {
                    if (!player.KnowsSpell(PowerWordShield) || player.HasBuff(PowerWordShield) || player.IsInCombat)
                    {
                        if (Wait.For("ShadowPriestPullDelay", 250))
                        {
                            player.SetTarget(target.Guid);
                            Wait.Remove("ShadowPriestPullDelay");

                            if (!player.IsInCombat)
                                player.LuaCall($"CastSpellByName('{pullingSpell}')");

                            player.StopAllMovement();
                            botStates.Pop();
                            botStates.Push(new CombatState(botStates, container, target));
                        }
                    }

                    if (player.KnowsSpell(PowerWordShield) && !player.HasDebuff(WeakenedSoul) && !player.HasBuff(PowerWordShield))
                        player.LuaCall($"CastSpellByName('{PowerWordShield}',1)");

                    return;
                }
            }
            else
            {
                stuckHelper.CheckIfStuck();

                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
        }
    }
}
