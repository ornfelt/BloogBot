using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Feral Druid Bot.
/// </summary>
namespace FeralDruidBot
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
        /// Represents the constant string "Bear Form".
        /// </summary>
        const string BearForm = "Bear Form";
        /// <summary>
        /// Represents the constant string "Cat Form".
        /// </summary>
        const string CatForm = "Cat Form";

        /// <summary>
        /// Represents the constant string "War Stomp".
        /// </summary>
        const string WarStomp = "War Stomp";
        /// <summary>
        /// Represents the constant string "Healing Touch".
        /// </summary>
        const string HealingTouch = "Healing Touch";

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
        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.target = target;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's actions based on certain conditions.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting) return;

            if (player.CurrentShapeshiftForm == BearForm && Wait.For("BearFormDelay", 1000, true))
                CastSpell(BearForm);

            if (player.CurrentShapeshiftForm == CatForm && Wait.For("CatFormDelay", 1000, true))
                CastSpell(CatForm);

            if (player.HealthPercent > 70 || player.Mana < player.GetManaCost(HealingTouch))
            {
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            if (player.IsSpellReady(WarStomp) && player.Position.DistanceTo(target.Position) <= 8)
                player.LuaCall($"CastSpellByName('{WarStomp}')");

            CastSpell(HealingTouch, castOnSelf: true);
        }

        /// <summary>
        /// Casts a spell with the specified name.
        /// </summary>
        /// <param name="name">The name of the spell to cast.</param>
        /// <param name="castOnSelf">Optional. Determines whether the spell should be cast on self. Default is false.</param>
        void CastSpell(string name, bool castOnSelf = false)
        {
            if (player.IsSpellReady(name))
            {
                var castOnSelfString = castOnSelf ? ",1" : "";
                player.LuaCall($"CastSpellByName('{name}'{castOnSelfString})");
            }
        }
    }
}
