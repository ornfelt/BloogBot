// Friday owns this file!

using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// Represents a state in which the bot is healing itself.
/// </summary>
namespace BeastMasterHunterBot
{
    /// <summary>
    /// Represents a state where the bot heals itself.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot heals itself.
    /// </summary>
    class HealSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Lesser Heal".
        /// </summary>
        const string LesserHeal = "Lesser Heal";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// The target GUID.
        /// </summary>
        readonly ulong targetGuid;

        /// <summary>
        /// Initializes a new instance of the HealSelfState class.
        /// </summary>
        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
            targetGuid = player.TargetGuid;
            player.SetTarget(player.Guid);
        }

        /// <summary>
        /// Updates the player's actions.
        /// </summary>
        public void Update()
        {
            //if (player.IsCasting) return;

            //if (player.HealthPercent > 70 || player.Mana < player.GetManaCost(LesserHeal))
            //{
            //    player.SetTarget(targetGuid);
            //    botStates.Pop();
            //    return;
            //}

            //player.LuaCall($"CastSpellByName('{LesserHeal}')");
        }
    }
}
