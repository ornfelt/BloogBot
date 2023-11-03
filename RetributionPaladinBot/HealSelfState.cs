using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Retribution Paladin Bot.
/// </summary>
namespace RetributionPaladinBot
{
    /// <summary>
    /// Represents a state where the bot heals itself.
    /// </summary>
    class HealSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Divine Protection".
        /// </summary>
        const string DivineProtection = "Divine Protection";
        /// <summary>
        /// Represents the constant string "Holy Light".
        /// </summary>
        const string HolyLight = "Holy Light";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the HealSelfState class.
        /// </summary>
        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's actions based on their current state.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting) return;

            if (player.HealthPercent > 70 || player.Mana < player.GetManaCost(HolyLight))
            {
                botStates.Pop();
                return;
            }

            if (player.Mana > player.GetManaCost(DivineProtection) && player.IsSpellReady(DivineProtection))
                player.LuaCall($"CastSpellByName('{DivineProtection}')");

            if (player.Mana > player.GetManaCost(HolyLight) && player.IsSpellReady(HolyLight))
                player.LuaCall($"CastSpellByName('{HolyLight}',1)");
        }
    }
}
