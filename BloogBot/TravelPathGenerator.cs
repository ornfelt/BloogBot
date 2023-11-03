using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// This class is responsible for generating travel paths.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a static class for generating travel paths.
    /// </summary>
    /// <summary>
    /// Represents a static class for generating travel paths.
    /// </summary>
    static public class TravelPathGenerator
    {
        /// <summary>
        /// TODO: this is wrong. we need a better way to notify the UI.
        /// </summary>
        // TODO: this is wrong. we need a better way to notify the UI.
        static Action callback;

        /// <summary>
        /// Represents the previous position.
        /// </summary>
        static Position previousPosition;
        /// <summary>
        /// The list of positions.
        /// </summary>
        static readonly IList<Position> positions = new List<Position>();

        /// <summary>
        /// Initializes the callback action.
        /// </summary>
        static public void Initialize(Action parCallback)
        {
            callback = parCallback;
        }

        /// <summary>
        /// Gets or sets a value indicating whether recording is enabled.
        /// </summary>
        static public bool Recording { get; private set; }

        /// <summary>
        /// Gets the count of positions.
        /// </summary>
        static public int PositionCount => positions.Count;

        /// <summary>
        /// Records the movement of a WoWPlayer and adds waypoints to a list if the player has moved more than 1 unit.
        /// </summary>
        static public async void Record(WoWPlayer player, Action<string> log)
        {
            Recording = true;
            previousPosition = player.Position;
            positions.Clear();

            while (Recording)
            {
                if (previousPosition.DistanceTo(player.Position) > 1)
                {
                    positions.Add(player.Position);
                    previousPosition = player.Position;
                    log("Adding waypoint " + positions.Count);
                }
                else
                    log("Player hasn't moved. Holding...");

                callback();
                await Task.Delay(1000);
            }
        }

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
