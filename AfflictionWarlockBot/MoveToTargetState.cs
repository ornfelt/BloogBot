using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Affliction Warlock bot.
/// </summary>
namespace AfflictionWarlockBot
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
        /// Represents the constant string value "Summon Imp".
        /// </summary>
        const string SummonImp = "Summon Imp";
        /// <summary>
        /// The constant string representing the action of summoning a Voidwalker.
        /// </summary>
        const string SummonVoidwalker = "Summon Voidwalker";
        /// <summary>
        /// Represents the constant string "Curse of Agony".
        /// </summary>
        const string CurseOfAgony = "Curse of Agony";
        /// <summary>
        /// Represents the constant string value "Shadow Bolt".
        /// </summary>
        const string ShadowBolt = "Shadow Bolt";

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

            if (player.KnowsSpell(CurseOfAgony))
                pullingSpell = CurseOfAgony;
            else
                pullingSpell = ShadowBolt;
        }

        /// <summary>
        /// Updates the current state of the bot. If the target is tapped by another player or if the closest target has a different GUID, stops all movement and pops the current state from the stack.
        /// If the player's pet is null and the player knows the SummonImp or SummonVoidwalker spell, stops all movement and pushes a new SummonVoidwalkerState to the stack.
        /// Checks if the player is stuck using the stuckHelper.
        /// Calculates the distance to the target and if it is less than 27, the player is not casting, and the pullingSpell is ready, stops all movement, casts the pullingSpell, pops the current state from the stack, and pushes a new CombatState to the stack.
        /// Gets the next waypoint using the Navigation class and moves the player toward it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Target: Check TappedByOther or FindClosestTarget
        /// Target --> Update: Return Guid
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> ObjectManager: Check Pet
        /// Update -> Player: KnowsSpell(SummonImp) or KnowsSpell(SummonVoidwalker)
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Push(new SummonVoidwalkerState)
        /// Update -> StuckHelper: CheckIfStuck
        /// Update -> Player: Get Position
        /// Update -> Target: Get Position
        /// Update -> Player: Check IsCasting and IsSpellReady
        /// Update -> Player: StopAllMovement
        /// Update -> Wait: For "AfflictionWarlockPullDelay"
        /// Update -> Player: StopAllMovement
        /// Update -> Player: LuaCall
        /// Update -> BotStates: Pop
        /// Update -> BotStates: Push(new CombatState)
        /// Update -> Navigation: GetNextWaypoint
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

            if (ObjectManager.Pet == null && (player.KnowsSpell(SummonImp) || player.KnowsSpell(SummonVoidwalker)))
            {
                player.StopAllMovement();
                botStates.Push(new SummonVoidwalkerState(botStates));
                return;
            }

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 27 && !player.IsCasting && player.IsSpellReady(pullingSpell))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For("AfflictionWarlockPullDelay", 250))
                {
                    player.StopAllMovement();
                    player.LuaCall($"CastSpellByName('{pullingSpell}')");
                    botStates.Pop();
                    botStates.Push(new CombatState(botStates, container, target));
                }

                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
