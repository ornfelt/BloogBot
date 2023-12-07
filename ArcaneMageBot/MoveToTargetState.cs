using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Arcane Mage Bot.
/// </summary>
namespace ArcaneMageBot
{
    /// <summary>
    /// Represents a class that handles the movement of the bot to a target state.
    /// </summary>
    /// <summary>
    /// Represents a class that implements the IBotState interface and handles moving to a target state.
    /// </summary>
    class MoveToTargetState : IBotState
    {
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
        }

        /// <summary>
        /// Updates the current state of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Target: Check TappedByOther or FindClosestTarget
        /// Target --> Update: Return Guid
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> StuckHelper: CheckIfStuck
        /// Update -> Player: Calculate DistanceToTarget
        /// Player --> Update: Return distance
        /// Update -> Player: Check IsMoving
        /// Player --> Update: Return IsMoving status
        /// Update -> Player: StopAllMovement
        /// Update -> Player: Check IsCasting and IsSpellReady
        /// Player --> Update: Return status
        /// Update -> Player: StopAllMovement
        /// Update -> Wait: RemoveAll
        /// Update -> Player: LuaCall
        /// Update -> BotStates: Pop
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

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 27)
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (!player.IsCasting && player.IsSpellReady(pullingSpell) && Wait.For("ArcaneMagePull", 500))
                {
                    player.StopAllMovement();
                    Wait.RemoveAll();
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
