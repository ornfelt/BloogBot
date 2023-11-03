using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes for handling travel paths.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a path for travel with associated waypoints.
    /// </summary>
    public class TravelPath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TravelPath"/> class.
        /// </summary>
        /// <param name="id">The ID of the travel path.</param>
        /// <param name="name">The name of the travel path.</param>
        /// <param name="waypoints">The collection of waypoints.</param>
        public TravelPath(int id, string name, IEnumerable<Position> waypoints)
        {
            Id = id;
            Name = name;
            Waypoints = waypoints.ToArray();
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
        /// Gets or sets the array of positions representing the waypoints.
        /// </summary>
        public Position[] Waypoints { get; }
    }
}
