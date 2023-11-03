using BloogBot.Game;

/// <summary>
/// Represents a hotspot in the game world that players can visit for various purposes.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a hotspot in the game.
    /// </summary>
    /// <summary>
    /// Represents a hotspot in the game.
    /// </summary>
    public class Hotspot
    {
        /// <summary>
        /// Initializes a new instance of the Hotspot class with the specified parameters.
        /// </summary>
        public Hotspot(
                    int id,
                    string zone,
                    string description,
                    string faction,
                    int minLevel,
                    Position[] waypoints,
                    Npc innkeeper,
                    Npc repairVendor,
                    Npc ammoVendor,
                    TravelPath travelPath,
                    bool safeForGrinding
                    )
        {
            Id = id;
            Zone = zone;
            Description = description;
            Faction = faction;
            MinLevel = minLevel;
            Waypoints = waypoints;
            Innkeeper = innkeeper;
            RepairVendor = repairVendor;
            AmmoVendor = ammoVendor;
            TravelPath = travelPath;
            SafeForGrinding = safeForGrinding;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the zone.
        /// </summary>
        public string Zone { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the faction of the object.
        /// </summary>
        public string Faction { get; }

        /// <summary>
        /// Gets the minimum level.
        /// </summary>
        public int MinLevel { get; }

        /// <summary>
        /// Gets or sets the array of positions representing the waypoints.
        /// </summary>
        public Position[] Waypoints { get; }

        /// <summary>
        /// Gets or sets the Innkeeper NPC.
        /// </summary>
        public Npc Innkeeper { get; }

        /// <summary>
        /// Gets or sets the repair vendor NPC.
        /// </summary>
        public Npc RepairVendor { get; }

        /// <summary>
        /// Gets or sets the AmmoVendor NPC.
        /// </summary>
        public Npc AmmoVendor { get; }

        /// <summary>
        /// Gets the travel path.
        /// </summary>
        public TravelPath TravelPath { get; }

        /// <summary>
        /// Gets a value indicating whether the object is safe for grinding.
        /// </summary>
        public bool SafeForGrinding { get; }

        /// <summary>
        /// Gets the display name of the object.
        /// </summary>
        public string DisplayName => $"{MinLevel} - {Zone}: {Description}";
    }
}
