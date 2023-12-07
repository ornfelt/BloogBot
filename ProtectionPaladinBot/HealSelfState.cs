using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Protection Paladin Bot.
/// </summary>
namespace ProtectionPaladinBot
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
        /// Updates the player's actions based on their current state and conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsCasting
        /// Player --> Update: return
        /// Update -> Player: HealthPercent > 70 || Mana < GetManaCost(HolyLight)
        /// Player --> Update: Pop botStates and return
        /// Update -> Player: Mana > GetManaCost(DivineProtection) && IsSpellReady(DivineProtection)
        /// Player --> Update: LuaCall(CastSpellByName(DivineProtection))
        /// Update -> Player: Mana > GetManaCost(HolyLight) && IsSpellReady(HolyLight)
        /// Player --> Update: LuaCall(CastSpellByName(HolyLight,1)) or CastSpell(HolyLight, Guid)
        /// \enduml
        /// </remarks>
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
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpellByName(\"HolyLight\",1)");
                }
                else
                {
                    player.CastSpell(HolyLight, player.Guid);
                }
            }
        }
    }
}
