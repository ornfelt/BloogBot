using BloogBot.Game;

/// <summary>
/// This namespace contains classes for handling non-player characters (NPCs) in the game.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a non-player character (NPC) in the game.
    /// </summary>
    /// <summary>
    /// Represents a non-player character (NPC) in the game.
    /// </summary>
    public class Npc
    {
        /// <summary>
        /// Initializes a new instance of the Npc class.
        /// </summary>
        public Npc(
                    int id,
                    string name,
                    bool isInnkeeper,
                    bool sellsAmmo,
                    bool repairs,
                    bool quest,
                    bool horde,
                    bool alliance,
                    Position position,
                    string zone
                    )
        {
            Id = id;
            Name = name;
            IsInnkeeper = isInnkeeper;
            SellsAmmo = sellsAmmo;
            Repairs = repairs;
            Quest = quest;
            Horde = horde;
            Alliance = alliance;
            Position = position;
            Zone = zone;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the person is an innkeeper.
        /// </summary>
        public bool IsInnkeeper { get; }

        /// <summary>
        /// Gets a value indicating whether the object sells ammo.
        /// </summary>
        public bool SellsAmmo { get; }

        /// <summary>
        /// Gets or sets a value indicating whether repairs are needed.
        /// </summary>
        public bool Repairs { get; }

        /// <summary>
        /// Gets or sets the value indicating whether the quest is completed.
        /// </summary>
        public bool Quest { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the Horde is present.
        /// </summary>
        public bool Horde { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the object belongs to an alliance.
        /// </summary>
        public bool Alliance { get; }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Gets the zone.
        /// </summary>
        public string Zone { get; }

        /// <summary>
        /// Gets the display name of the character, including the faction.
        /// </summary>
        public string DisplayName
        {
            get
            {
                string faction;
                if (Horde && Alliance)
                    faction = "H/A";
                else if (Horde)
                    faction = "H";
                else
                    faction = "A";

                return $"{Name} - {faction}";
            }
        }
    }
}
