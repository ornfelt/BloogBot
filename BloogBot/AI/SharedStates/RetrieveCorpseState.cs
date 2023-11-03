using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains shared states for the AI of the BloogBot bot. 
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot retrieves a corpse.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot retrieves a corpse.
    /// </summary>
    public class RetrieveCorpseState : IBotState
    {
        /// <summary>
        /// The constant integer value representing the distance of 25.
        /// </summary>
        const int resDistance = 25;
        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Builds up a grid of 38 units in every direction, adding 1 to account for the center.
        /// </summary>
        // res distance is around 36 units, so we build up a grid of 38 units 
        // in every direction, adding 1 to account for the center.
        static readonly int length = Convert.ToInt32(Math.Pow((resDistance * 2) + 1, 2.0));
        /// <summary>
        /// Array of positions with a fixed length.
        /// </summary>
        readonly Position[] resLocs = new Position[length];
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Represents a boolean value indicating whether the object has been initialized.
        /// </summary>
        bool initialized;

        /// <summary>
        /// Initializes a new instance of the RetrieveCorpseState class.
        /// </summary>
        public RetrieveCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the state of the bot. Resets WpStuckCount and checks if the player is in ghost form. If not, pops the current state. 
        /// If the bot is not initialized, waits for 5 seconds after releasing the corpse and then calculates the best resurrection location based on threats and player level. 
        /// Once initialized, waits for a delay and then retrieves the player's corpse if still in ghost form. If not, pops the current state.
        /// </summary>
        public void Update()
        {
            player.WpStuckCount = 0; // Reset WpStuckCount
            if (!player.InGhostForm)
            {
                botStates.Pop();
                return;
            }
            if (!initialized)
            {
                // corpse position is wrong immediately after releasing, so we wait for 5s.
                //Thread.Sleep(5000);

                var resLocation = player.CorpsePosition;

                var threats = ObjectManager
                    .Units
                    .Where(u => u.Health > 0)
                    .Where(u => !u.TappedByOther)
                    .Where(u => !u.IsPet)
                    .Where(u => u.UnitReaction == UnitReaction.Hated || u.UnitReaction == UnitReaction.Hostile)
                    .Where(u => u.Level > player.Level - 10);

                if (threats.FirstOrDefault() != null)
                {
                    var index = 0;
                    var currentFloatX = player.CorpsePosition.X;
                    var currentFloatY = player.CorpsePosition.Y;
                    var currentFloatZ = player.CorpsePosition.Z;

                    for (var i = -resDistance; i <= resDistance; i++)
                    {
                        for (var j = -resDistance; j <= resDistance; j++)
                        {
                            resLocs[index] = new Position(currentFloatX + i, currentFloatY + j, currentFloatZ);
                            index++;
                        }
                    }

                    // Reslocations are > 300 so the threat search takes a long time to execute
                    Console.WriteLine("Reslocations: " + resLocs.Length);
                    var maxDistance = 0f;
                    foreach (var resLoc in resLocs)
                    {
                        var path = Navigation.CalculatePath(ObjectManager.MapId, player.CorpsePosition, resLoc, false);
                        if (path.Length == 0) continue;
                        var endPoint = path[path.Length - 1];
                        var distanceToClosestThreat = endPoint.DistanceTo(threats.OrderBy(u => u.Position.DistanceTo(resLoc)).First().Position);

                        if (endPoint.DistanceTo(player.Position) < resDistance && distanceToClosestThreat > maxDistance)
                        {
                            maxDistance = distanceToClosestThreat;
                            resLocation = resLoc;
                        }
                    }
                }

                initialized = true;

                botStates.Push(new MoveToPositionState(botStates, container, resLocation, true));
                return;
            }

            if (Wait.For("StartRetrieveCorpseStateDelay", 1000))
            {
                if (ObjectManager.Player.InGhostForm)
                    ObjectManager.Player.RetrieveCorpse();
                else
                {
                    if (Wait.For("LeaveRetrieveCorpseStateDelay", 2000))
                    {
                        botStates.Pop();
                        return;
                    }
                }
            }
        }
    }
}
