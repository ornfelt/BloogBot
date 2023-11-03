using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class is responsible for generating hotspots.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a class for generating hotspots.
    /// </summary>
    /// <summary>
    /// Represents a class that generates hotspots.
    /// </summary>
    static public class HotspotGenerator
    {
        /// <summary>
        /// Represents a collection of positions.
        /// </summary>
        static IList<Position> positions = new List<Position>();

        /// <summary>
        /// Gets or sets a value indicating whether recording is enabled.
        /// </summary>
        static public bool Recording { get; private set; }

        /// <summary>
        /// Gets the count of positions.
        /// </summary>
        static public int PositionCount => positions.Count;

        /// <summary>
        /// Sets the Recording flag to true and clears the positions list.
        /// </summary>
        static public void Record()
        {
            Recording = true;
            positions.Clear();
        }

        /// <summary>
        /// Adds a waypoint to the list of positions.
        /// </summary>
        static public void AddWaypoint(Position position) => positions.Add(position);

        /// <summary>
        /// Cancels the recording process by setting the Recording flag to false.
        /// </summary>
        static public void Cancel() => Recording = false;

        /// <summary>
        /// Saves the current positions and returns an array of Position objects.
        /// </summary>
        static public Position[] Save()
        {
            Recording = false;
            return positions.ToArray();
        }
    }
}
