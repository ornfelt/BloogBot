// Ported from BloogBot/BloogBot/GatherRoute.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
namespace BloogBot
{
    public class GatherRoute
    {
        public GatherRoute(int id, string name, string nodeNames, TravelPath travelPath)
        {
            Id = id;
            Name = name;
            TravelPath = travelPath;
            NodeNames = nodeNames;
        }

        public int Id { get; }

        public string Name { get; }

        public TravelPath TravelPath { get; }

        public string NodeNames { get; }
    }
}
