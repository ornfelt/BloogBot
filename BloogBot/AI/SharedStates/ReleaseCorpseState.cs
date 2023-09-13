using BloogBot.Game;
using System;
using System.Collections.Generic;

namespace BloogBot.AI.SharedStates
{
    public class ReleaseCorpseState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;

        public ReleaseCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
        }

        public void Update()
        {
            if (Wait.For("StartReleaseCorpseStateDelay", 1000))
            {
                if (!ObjectManager.Player.InGhostForm)
                    ObjectManager.Player.ReleaseCorpse();
                else
                {
                    if (Wait.For("LeaveReleaseCorpseStateDelay", 2000))
                    {
                        botStates.Pop();
                        if (ObjectManager.Player.DeathsAtWp > 3)
                        {
                            Console.WriteLine("Forcing teleport to WP after release due to deathcount");
                            ObjectManager.Player.LuaCall($"SendChatMessage('.npcb wp go {ObjectManager.Player.CurrWpId}')");
                        }
                        return;
                    }
                }
            }
        }
    }
}
