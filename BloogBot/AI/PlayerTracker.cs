using System;

/// <summary>
/// This namespace contains classes for tracking player behavior and interactions.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents a class that tracks players.
    /// </summary>
    /// <summary>
    /// Represents a class that tracks players.
    /// </summary>
    public class PlayerTracker
    {
        /// <summary>
        /// Initializes a new instance of the PlayerTracker class with the specified first seen value.
        /// </summary>
        public PlayerTracker(int firstSeen)
        {
            FirstSeen = firstSeen;
        }

        /// <summary>
        /// Gets the tick count of the system when this property was first accessed.
        /// </summary>
        public int FirstSeen { get; } = Environment.TickCount;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is targeting me.
        /// </summary>
        public bool TargetingMe { get; set; }

        /// <summary>
        /// Gets or sets the first targeted me value.
        /// </summary>
        public int FirstTargetedMe { get; set; } = Environment.TickCount;

        /// <summary>
        /// Gets or sets a value indicating whether the target warning is enabled.
        /// </summary>
        public bool TargetWarning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is a proximity warning.
        /// </summary>
        public bool ProximityWarning { get; set; }
    }
}
