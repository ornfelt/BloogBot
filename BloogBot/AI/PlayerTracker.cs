// Ported from BloogBot/BloogBot/AI/PlayerTracker.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System;

namespace BloogBot.AI
{
    public class PlayerTracker
    {
        public PlayerTracker(int firstSeen)
        {
            FirstSeen = firstSeen;
        }

        public int FirstSeen { get; } = Environment.TickCount;

        public bool TargetingMe { get; set; }

        public int FirstTargetedMe { get; set; } = Environment.TickCount;

        public bool TargetWarning { get; set; }

        public bool ProximityWarning { get; set; }
    }
}
