using System.Text;

/// <summary>
/// This namespace contains classes related to game spells.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a spell with an id, cost, name, description, and tooltip.
    /// </summary>
    /// <summary>
    /// Represents a spell with an id, cost, name, description, and tooltip.
    /// </summary>
    public class Spell
    {
        /// <summary>
        /// Initializes a new instance of the Spell class with the specified id, cost, name, description, and tooltip.
        /// </summary>
        public Spell(int id, int cost, string name, string description, string tooltip)
        {
            Id = id;
            Cost = cost;
            Name = name;
            Description = description;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the cost.
        /// </summary>
        public int Cost { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets the tooltip for the object.
        /// </summary>
        public string Tooltip { get; }

        /// <summary>
        /// Returns a string representation of the Spell object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Spell {Id}:");
            sb.AppendLine($"  Cost: {Cost}");
            sb.AppendLine($"  Name: {Name}");
            sb.AppendLine($"  Description: {Description}");
            sb.AppendLine($"  Tooltip: {Tooltip}");
            return sb.ToString();
        }
    }
}
