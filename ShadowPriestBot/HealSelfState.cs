using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Shadow Priest Bot.
/// </summary>
namespace ShadowPriestBot
{
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
        /// Represents a constant string for healing.
        /// </summary>
        const string Heal = "Heal";
        /// <summary>
        /// Represents the constant string "Renew".
        /// </summary>
        const string Renew = "Renew";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Gets or sets the healing spell.
        /// </summary>
        readonly string healingSpell;

        /// <summary>
        /// Initializes a new instance of the HealSelfState class.
        /// </summary>
        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;

            if (player.KnowsSpell(Heal))
                healingSpell = Heal;
            else
                healingSpell = LesserHeal;
        }

        /// <summary>
        /// Updates the player's actions based on their health and mana levels.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting) return;

            if (player.HealthPercent > 70 || player.Mana < player.GetManaCost(healingSpell))
            {
                if (player.KnowsSpell(Renew) && player.Mana > player.GetManaCost(Renew))
                    player.LuaCall($"CastSpellByName('{Renew}',1)");

                botStates.Pop();
                return;
            }

            player.LuaCall($"CastSpellByName('{healingSpell}',1)");
        }
    }
}
