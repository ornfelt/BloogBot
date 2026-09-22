// Ported from BloogBot/BloogBot/AI/SharedStates/ReleaseCorpseState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
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
#if USE_CUSTOM_CHANGES
        static readonly Random random = new Random();
#endif

        public ReleaseCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
        }

        public void Update()
        {
            if (Wait.For("StartReleaseCorpseStateDelay", 1000))
            {
#if USE_CUSTOM_CHANGES
                if (!ObjectManager.Player.InGhostForm && ObjectManager.Player.Health <= 0)
                    ObjectManager.Player.ReleaseCorpse();
                else if (!ObjectManager.Player.InGhostForm && ObjectManager.Player.Health > 0)
                {
                    botStates.Pop();
                    return;
                }
                else
                {
                    var mapId = ObjectManager.MapId;
                    if (Wait.For("LeaveReleaseCorpseStateDelay", (mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559) ? 30000 : 2000))
                    {
                        botStates.Pop();
                        return;
                    }
                }
#else
                if (ObjectManager.Player.InGhostForm || ObjectManager.Player.Health > 0)
                {
                    if (Wait.For("LeaveReleaseCorpseStateDelay", 2000))
                    {
                        botStates.Pop();
                        return;
                    }
                }
                else
                {
                    ObjectManager.Player.ReleaseCorpse();

                }
#endif
            }
        }
    }
}
