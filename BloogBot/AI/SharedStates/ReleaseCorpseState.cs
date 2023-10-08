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
                    var mapId = ObjectManager.MapId;
                    // If in BG
                    if (mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559)
                    {
                        if (Wait.For("LeaveReleaseCorpseStateDelay", 30000) || !ObjectManager.Player.InGhostForm)
                        {
                            botStates.Pop();
                            return;
                        }
                    }
                    else
                    {
                        if (Wait.For("LeaveReleaseCorpseStateDelay", 2000))
                        {
                            botStates.Pop();
                            return;
                        }
                    }
                }
            }
        }
    }
}
