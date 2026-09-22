// Ported from BloogBot/BloogBot/TravelPath.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot
{
    public class TravelPath
    {
        public TravelPath(int id, string name, IEnumerable<Position> waypoints)
        {
            Id = id;
            Name = name;
            Waypoints = waypoints.ToArray();
        }

        public int Id { get; }

        public string Name { get; }

        public Position[] Waypoints { get; }
    }
}
