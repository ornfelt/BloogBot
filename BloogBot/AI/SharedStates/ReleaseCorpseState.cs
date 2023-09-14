using BloogBot.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BloogBot.AI.SharedStates
{
    public class ReleaseCorpseState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        static readonly Random random = new Random();

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
                        ObjectManager.Player.WpStuckCount = 0;
                        botStates.Pop();
                        return;
                    }
                }
            }
        }
    }
}
