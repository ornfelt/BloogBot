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
        /// <remarks>
        /// \startuml
        /// participant "Record Method" as R
        /// R -> "Recording Variable": Set to true
        /// R -> "Positions List": Clear
        /// \enduml
        /// </remarks>
        static public void Record()
        {
            Recording = true;
            positions.Clear();
        }

        /// <summary>
        /// Adds a waypoint to the list of positions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Position" as P
        /// participant "positions" as PS
        /// P -> PS: Add(position)
        /// \enduml
        /// </remarks>
        static public void AddWaypoint(Position position) => positions.Add(position);

        /// <summary>
        /// Cancels the recording process by setting the Recording flag to false.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> Cancel: Invoke Cancel()
        /// Cancel --> :User: : Recording = false
        /// \enduml
        /// </remarks>
        static public void Cancel() => Recording = false;

        /// <summary>
        /// Saves the current positions and returns an array of Position objects.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Save Method" as Save
        /// participant "Position Array" as Positions
        /// Save -> Positions: Convert positions to array
        /// \enduml
        /// </remarks>
        static public Position[] Save()
        {
            Recording = false;
            return positions.ToArray();
        }
    }
}
