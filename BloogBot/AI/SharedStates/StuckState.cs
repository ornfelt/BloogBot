using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// This namespace contains shared states for the AI of the BloogBot bot, including the StuckState class.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot is stuck and unable to proceed.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot is stuck and unable to proceed.
    /// </summary>
    public class StuckState : IBotState
    {
        /// <summary>
        /// Represents a stopwatch that can measure elapsed time.
        /// </summary>
        static readonly Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Gets the starting position.
        /// </summary>
        readonly Position startingPosition;

        /// <summary>
        /// Sets the state of the object to "Stuck".
        /// </summary>
        State state = State.Stuck;

        /// <summary>
        /// Initializes a new instance of the StuckState class.
        /// </summary>
        public StuckState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            startingPosition = player.Position;
        }

        /// <summary>
        /// Updates the movement of the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: Get WpStuckCount
        /// Update -> Player: Check InGhostForm
        /// Update -> Container: GetCurrentHotspot
        /// Update -> Player: Get CurrWpId
        /// Update -> Player: Get Position
        /// Update -> Player: Check IsInCombat
        /// Update -> Update: StopMovement
        /// Update -> BotStates: Pop
        /// Update -> Update: Check state
        /// Update -> Update: Calculate moveTime
        /// Update -> Update: Check stopwatch
        /// Update -> Update: Change state to Stuck
        /// Update -> Update: Generate random number
        /// Update -> Update: Change state to Moving
        /// Update -> Update: Restart stopwatch
        /// Update -> Update: StopMovement
        /// Update -> Player: StartMovement
        /// Update -> Player: Jump
        /// \enduml
        /// </remarks>
        public void Update()
        {
            var wpStuckCount = player.WpStuckCount + 1;
            var posDistance = wpStuckCount < 5 || (player.InGhostForm && wpStuckCount > 10) ? 3 : random.Next(wpStuckCount, (wpStuckCount * 20));
            var currWp = container.GetCurrentHotspot().Waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();
            var wpDistance = currWp == null ? 100 : player.Position.DistanceTo(currWp);

            if (player.Position.DistanceTo(startingPosition) > posDistance || player.IsInCombat
                || wpDistance < 3)
            {
                StopMovement();
                botStates.Pop();
                return;
            }

            if (state == State.Moving)
            {
                var moveTime = (100 * posDistance) / 3;
                if (stopwatch.ElapsedMilliseconds > moveTime)
                    state = State.Stuck;
                return;
            }

            var ran = random.Next(0, 4);
            state = State.Moving;
            stopwatch.Restart();
            StopMovement();

            if (ran == 0)
            {
                player.StartMovement(ControlBits.Front);
                player.StartMovement(ControlBits.StrafeLeft);
                player.Jump();
            }
            if (ran == 1)
            {
                player.StartMovement(ControlBits.Front);
                player.StartMovement(ControlBits.StrafeRight);
                player.Jump();
            }
            if (ran == 2)
            {
                player.StartMovement(ControlBits.Back);
                player.StartMovement(ControlBits.StrafeLeft);
                player.Jump();
            }
            if (ran == 3)
            {
                player.StartMovement(ControlBits.Back);
                player.StartMovement(ControlBits.StrafeRight);
                player.Jump();
            }
        }

        /// <summary>
        /// Stops the movement of the player in all directions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// StopMovement -> Player: StopMovement(ControlBits.Front)
        /// StopMovement -> Player: StopMovement(ControlBits.Back)
        /// StopMovement -> Player: StopMovement(ControlBits.StrafeLeft)
        /// StopMovement -> Player: StopMovement(ControlBits.StrafeRight)
        /// \enduml
        /// </remarks>
        void StopMovement()
        {
            player.StopMovement(ControlBits.Front);
            player.StopMovement(ControlBits.Back);
            player.StopMovement(ControlBits.StrafeLeft);
            player.StopMovement(ControlBits.StrafeRight);
        }

        /// <summary>
        /// Represents the possible states of an object.
        /// </summary>
        enum State
        {
            Stuck,
            Moving
        }
    }
}
