using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the implementation of the HealSelfState class, which handles healing actions for a Balance Druid bot.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents the HealSelfState class.
    /// </summary>
    /// <summary>
    /// Represents the HealSelfState class.
    /// </summary>
    class HealSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "War Stomp".
        /// </summary>
        const string WarStomp = "War Stomp";
        /// <summary>
        /// Represents the constant string "Healing Touch".
        /// </summary>
        const string HealingTouch = "Healing Touch";
        /// <summary>
        /// The constant string representing "Rejuvenation".
        /// </summary>
        const string Rejuvenation = "Rejuvenation";
        /// <summary>
        /// Represents the constant string "Barkskin".
        /// </summary>
        const string Barkskin = "Barkskin";
        /// <summary>
        /// Represents the constant string "Moonkin Form".
        /// </summary>
        const string MoonkinForm = "Moonkin Form";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the HealSelfState class.
        /// </summary>
        public HealSelfState(Stack<IBotState> botStates, WoWUnit target)
        {
            this.botStates = botStates;
            this.target = target;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's actions based on their current state and conditions.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting) return;

            if (player.HealthPercent > 70 || (player.Mana < player.GetManaCost(HealingTouch) && player.Mana < player.GetManaCost(Rejuvenation)))
            {
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            if (player.IsSpellReady(WarStomp) && player.Position.DistanceTo(target.Position) <= 8)
                player.LuaCall($"CastSpellByName('{WarStomp}')");

            TryCastSpell(MoonkinForm, player.HasBuff(MoonkinForm));

            TryCastSpell(Barkskin);

            TryCastSpell(Rejuvenation, !player.HasBuff(Rejuvenation));

            TryCastSpell(HealingTouch);
        }

        /// <summary>
        /// Tries to cast a spell by name if the spell is ready and the condition is true.
        /// </summary>
        void TryCastSpell(string name, bool condition = true)
        {
            if (player.IsSpellReady(name) && condition)
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
