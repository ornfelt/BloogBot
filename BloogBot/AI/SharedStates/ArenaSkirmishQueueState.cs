using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the state of the bot when queuing for arena skirmishes.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state for the Arena Skirmish queue.
    /// </summary>
    /// <summary>
    /// Represents a state for the Arena Skirmish queue.
    /// </summary>
    public class ArenaSkirmishQueueState : IBotState
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
        /// Represents a local player.
        /// </summary>
        LocalPlayer player;
        /// <summary>
        /// Represents the current state of the ArenaQueue.
        /// </summary>
        private ArenaQueueStates currentState;
        /// <summary>
        /// Represents a static random number generator.
        /// </summary>
        private static Random rand = new Random();

        /// <summary>
        /// Initializes a new instance of the ArenaSkirmishQueueState class.
        /// </summary>
        public ArenaSkirmishQueueState(
                    Stack<IBotState> botStates,
                    IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the current state of the arena queue process.
        /// </summary>
        public void Update()
        {
            player = ObjectManager.Player;

            if (currentState == ArenaQueueStates.Initial)
            {
                player.StopAllMovement();
                player.LuaCall($"SendChatMessage('.go creature {(IsAlly() ? "68938" : "4762")}')");
                currentState = ArenaQueueStates.BotTeleported;
                player.ShouldWaitForTeleportDelay = true;
                return;
            }

            if (currentState == ArenaQueueStates.BotTeleported && Wait.For("BotTeleportedDelay", 1500))
            {
                bool isAlly = IsAlly();
                var arenaNpc = ObjectManager.Units.Where(u => u.Name == (isAlly ? "Beka Zipwhistle" : "Zeggon Botsnap")).FirstOrDefault();
                if (arenaNpc != null)
                    player.SetTarget(arenaNpc.Guid);
                currentState = ArenaQueueStates.NpcTargeted;
                return;
            }

            if (currentState == ArenaQueueStates.NpcTargeted && Wait.For("NpcTargetedDelay", 1500))
            {
                var arenaNpc = ObjectManager.Units.Where(u => u.Name == (IsAlly() ? "Beka Zipwhistle" : "Zeggon Botsnap")).FirstOrDefault();
                if (arenaNpc != null)
                    arenaNpc.Interact();
                currentState = ArenaQueueStates.NpcInteractedWith;
                return;
            }

            if (currentState == ArenaQueueStates.NpcInteractedWith && Wait.For("NpcInteractedWithDelay", 1500))
            {
                int bgQueueIndex = rand.Next(3) + 1;
                Console.WriteLine($"Queueing for arena: {bgQueueIndex}");
                player.LuaCall($"JoinBattlefield({bgQueueIndex},0)");
                currentState = ArenaQueueStates.ArenaQueued;
                return;
            }

            if (currentState == ArenaQueueStates.ArenaQueued && Wait.For("ArenaQueuedDelay", 5000))
            {
                player.LuaCall($"AcceptBattlefieldPort(1,1)");
                currentState = ArenaQueueStates.ArenaJoined;
                player.HasJoinedBg = true;
                botStates.Pop();
                return;
            }
        }

        /// <summary>
        /// Determines if the player is an ally.
        /// </summary>
        private bool IsAlly()
        {
            // TODO: determine faction
            Console.WriteLine($"Player faction: {player.FactionId}");
            return false;
        }
    }

    /// <summary>
    /// Represents the possible states of an arena queue.
    /// </summary>
    enum ArenaQueueStates
    {
        Initial,
        BotTeleported,
        NpcTargeted,
        NpcInteractedWith,
        ArenaQueued,
        ArenaJoined,
    }
}
