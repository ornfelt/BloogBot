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
                        Thread.Sleep(500);
                        var player = ObjectManager.Player;
                        if (player.DeathsAtWp > 2)
                        {
                            // Select new waypoint based on links
                            var hotspot = container.GetCurrentHotspot();
                            var waypoint = hotspot.Waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();
                            string wpLinks = waypoint.Links.Replace(":0", "");
                            if (wpLinks.EndsWith(" "))
                                wpLinks = wpLinks.Remove(wpLinks.Length - 1);
                            string[] linkSplit = wpLinks.Split(' ');
                            int randLink = random.Next() % linkSplit.Length;
                            var linkWp = hotspot.Waypoints.Where(x => x.ID == Int32.Parse(linkSplit[randLink])).FirstOrDefault();

                            Console.WriteLine($"Forcing teleport to linked WP: {linkWp.ID} after release due to deathcount > 2");
                            ObjectManager.Player.LuaCall($"SendChatMessage('.npcb wp go {linkWp.ID}')");
                        }
                        botStates.Pop();
                        return;
                    }
                }
            }
        }
    }
}
