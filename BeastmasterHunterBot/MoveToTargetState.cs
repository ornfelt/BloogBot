// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the implementation of the MoveToTargetState class, which represents a state in the BeastMasterHunterBot.
/// </summary>
namespace BeastMasterHunterBot
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
        /// The Lua script for the gun, which checks if the action is set to auto-repeat and casts the 'Auto Shot' spell if not.
        /// </summary>
        const string GunLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Auto Shot') end";
        /// <summary>
        /// Represents the constant string "Serpent Sting".
        /// </summary>
        const string SerpentSting = "Serpent Sting";
        /// <summary>
        /// Represents the constant string value for "Aspect Of The Monkey".
        /// </summary>
        const string AspectOfTheMonkey = "Aspect Of The Monkey";
        /// <summary>
        /// Represents the constant string value for "Aspect Of The Cheetah".
        /// </summary>
        const string AspectOfTheCheetah = "Aspect Of The Cheetah";




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
        /// Updates the current state of the bot. If the target is tapped by another player or if the closest target has a different GUID, stops all movement and pops the current state from the stack.
        /// Checks if the player is stuck using the stuckHelper.
        /// If the distance to the target is less than 33 and the player is not currently casting, stops all movement, calls the GunLuaScript, pops the current state from the stack, and pushes a new CombatState onto the stack.
        /// Gets the next waypoint using the Navigation class and moves the player towards it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Target: Check TappedByOther or FindClosestTarget
        /// Target --> Update: Return result
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> StuckHelper: CheckIfStuck
        /// Update -> Player: Calculate DistanceTo
        /// Player --> Update: Return distance
        /// Update -> Player: StopAllMovement
        /// Update -> Player: LuaCall(GunLuaScript)
        /// Update -> BotStates: Pop
        /// Update -> BotStates: Push(new CombatState)
        /// Update -> Navigation: GetNextWaypoint
        /// Navigation --> Update: Return nextWaypoint
        /// Update -> Player: MoveToward(nextWaypoint)
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

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 33 && !player.IsCasting)
            {
                player.StopAllMovement();
                player.LuaCall(GunLuaScript);
                botStates.Pop();
                botStates.Push(new CombatState(botStates, container, target));
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
