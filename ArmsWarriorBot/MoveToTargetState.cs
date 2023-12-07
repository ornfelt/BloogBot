using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Arms Warrior bot.
/// </summary>
namespace ArmsWarriorBot
{
    /// <summary>
    /// Represents a class that handles moving to a target state in a bot.
    /// </summary>
    /// <summary>
    /// Represents a class that handles moving to a target state in a bot.
    /// </summary>
    class MoveToTargetState : IBotState
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
        /// Updates the behavior of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Target: Check TappedByOther or FindClosestTarget
        /// Target --> Update: Return status
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop state
        /// Update -> Player: Check IsInCombat
        /// Player --> Update: Return status
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop state
        /// Update -> BotStates: Push new CombatState
        /// Update -> StuckHelper: CheckIfStuck
        /// Update -> Player: Calculate DistanceTo target
        /// Player --> Update: Return distance
        /// Update -> Player: Check IsCasting, IsSpellReady and InLosWith
        /// Player --> Update: Return status
        /// Update -> Player: LuaCall or CastSpell
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop state
        /// Update -> BotStates: Push new CombatState
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

            if (player.IsInCombat)
            {
                player.StopAllMovement();
                botStates.Pop();
                botStates.Push(new CombatState(botStates, container, target));
                return;
            }

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 25 && distanceToTarget > 8 && !player.IsCasting && player.IsSpellReady("Charge") && player.InLosWith(target.Position))
            {
                if (!player.IsCasting)
                {
                    if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                    {
                        player.LuaCall($"CastSpellByName('Charge')");
                    }
                    else
                    {
                        player.CastSpell("Charge", target.Guid);
                    }
                }
            }

            if (distanceToTarget < 3)
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
