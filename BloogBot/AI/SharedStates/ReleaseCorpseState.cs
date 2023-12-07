using BloogBot.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// This namespace contains shared states for AI behavior in the BloogBot project.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot releases a corpse.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot releases a corpse.
    /// </summary>
    public class ReleaseCorpseState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Initializes a new instance of the ReleaseCorpseState class.
        /// </summary>
        public ReleaseCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
        }

        /// <summary>
        /// Updates the game state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Wait: For("StartReleaseCorpseStateDelay", 1000)
        /// Wait --> Update: Response
        /// alt Response is True
        ///     Update -> ObjectManager.Player: InGhostForm
        ///     Update -> ObjectManager.Player: Health
        ///     alt Player is not in GhostForm and Health <= 0
        ///         Update -> ObjectManager.Player: ReleaseCorpse()
        ///     else Player is not in GhostForm and Health > 0
        ///         Update -> botStates: Pop()
        ///         Update --> Update: return
        ///     else
        ///         Update -> ObjectManager: MapId
        ///         Update -> Wait: For("LeaveReleaseCorpseStateDelay", (mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559) ? 30000 : 2000)
        ///         alt Response is True
        ///             Update -> botStates: Pop()
        ///             Update --> Update: return
        ///         end
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (Wait.For("StartReleaseCorpseStateDelay", 1000))
            {
                if (!ObjectManager.Player.InGhostForm && ObjectManager.Player.Health <= 0)
                    ObjectManager.Player.ReleaseCorpse();
                else if (!ObjectManager.Player.InGhostForm && ObjectManager.Player.Health > 0)
                {
                    botStates.Pop();
                    return;
                }
                else
                {
                    var mapId = ObjectManager.MapId;
                    if (Wait.For("LeaveReleaseCorpseStateDelay", (mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559) ? 30000 : 2000))
                    {
                        botStates.Pop();
                        return;
                    }
                }
            }
        }
    }
}
