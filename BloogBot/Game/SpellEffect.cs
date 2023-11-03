using BloogBot.Game.Enums;

/// <summary>
/// This namespace contains classes related to game spell effects.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a spell effect in a game, including its icon, stack count, and effect type.
    /// </summary>
    public class SpellEffect
    {
        /// <summary>
        /// Initializes a new instance of the SpellEffect class with the specified icon, stack count, and effect type.
        /// </summary>
        public SpellEffect(string icon, int stackCount, EffectType type)
        {
            Icon = icon;
            StackCount = stackCount;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the icon for the object.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public int StackCount { get; }

        /// <summary>
        /// Gets the type of the effect.
        /// </summary>
        public EffectType Type { get; }
    }
}
