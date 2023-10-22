using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot.AI.SharedStates
{
    public class ArenaSkirmishQueueState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        LocalPlayer player;
        private ArenaQueueStates currentState;
        private static Random rand = new Random();

        public ArenaSkirmishQueueState(
            Stack<IBotState> botStates,
            IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (CheckCombat())
                return;
            player = ObjectManager.Player;

            if (currentState == ArenaQueueStates.Initial)
            {
                player.StopAllMovement();
                player.LuaCall($"SendChatMessage('.go creature {(IsAlly() ? "68938": "4762")}')");
                currentState = ArenaQueueStates.BotTeleported;
                return;
            }

            if (currentState == ArenaQueueStates.BotTeleported && Wait.For("BotTeleportedDelay", 4500))
            {
                var arenaNpc = ObjectManager.Units.Where(u => u.Name == (IsAlly() ? "Beka Zipwhistle" : "Zeggon Botsnap")).FirstOrDefault();
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
                int bgQueueIndex = rand.Next(2) + 1;
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

        private bool IsAlly()
        {
            // TODO: determine faction
            Console.WriteLine($"Player faction: {player.FactionId}");
            return false;
        }

        private bool CheckCombat()
        {
            if (player.IsInCombat)
            {
                botStates.Pop();
                botStates.Push(new GrindState(botStates, container));
                return true;
            }
            return false;
        }
    }

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
