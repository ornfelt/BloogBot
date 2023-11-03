using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains shared states for the AI system in BloogBot.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a class that gathers the state of an object.
    /// </summary>
    /// <summary>
    /// Represents a class that gathers the state of an object.
    /// </summary>
    class GatherObjectState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only World of Warcraft game object target.
        /// </summary>
        readonly WoWGameObject target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents the initial count value.
        /// </summary>
        readonly int initialCount = 0;

        /// <summary>
        /// Represents the start time of the application in milliseconds since the system started.
        /// </summary>
        readonly int startTime = Environment.TickCount;

        /// <summary>
        /// Initializes a new instance of the GatherObjectState class.
        /// </summary>
        internal GatherObjectState(Stack<IBotState> botStates, IDependencyContainer container, WoWGameObject target)
        {
            this.botStates = botStates;
            this.target = target;
            player = ObjectManager.Player;
            initialCount = Inventory.GetItemCount(target.Name);
        }

        /// <summary>
        /// Updates the state of the bot.
        /// </summary>
        public void Update()
        {
            if (player.IsInCombat || (Environment.TickCount - startTime > 15000))
            {
                botStates.Pop();
                return;
            }

            if (Wait.For("InteractWithObjectDelay", 15000, true))
                target.Interact();

            if (Inventory.GetItemCount(target.Name) > initialCount)
            {
                if (Wait.For("PopGatherObjectStateDelay", 2000))
                {
                    Wait.RemoveAll();
                    botStates.Pop();
                    return;
                }
            }
        }
    }
}
