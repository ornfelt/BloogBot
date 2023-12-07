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
    /// Represents a state where the bot buffs itself with various blessings.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot buffs itself with various blessings.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Blessing of Kings".
        /// </summary>
        const string BlessingOfKings = "Blessing of Kings";
        /// <summary>
        /// Represents the constant string "Blessing of Might".
        /// </summary>
        const string BlessingOfMight = "Blessing of Might";
        /// <summary>
        /// Represents the constant string "Blessing of Sanctuary".
        /// </summary>
        const string BlessingOfSanctuary = "Blessing of Sanctuary";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuffSelfState"/> class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's buffs by casting appropriate spells.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Update -> Player: KnowsSpell(BlessingOfMight), HasBuff(BlessingOfMight), HasBuff(BlessingOfKings), HasBuff(BlessingOfSanctuary)
        /// alt player does not know BlessingOfMight or has any of the buffs
        ///     Update -> BotStates: Pop()
        /// else player knows BlessingOfMight and does not know BlessingOfKings and BlessingOfSanctuary
        ///     Update -> Update: TryCastSpell(BlessingOfMight)
        /// else player knows BlessingOfKings and does not know BlessingOfSanctuary
        ///     Update -> Update: TryCastSpell(BlessingOfKings)
        /// else player knows BlessingOfSanctuary
        ///     Update -> Update: TryCastSpell(BlessingOfSanctuary)
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (!player.KnowsSpell(BlessingOfMight) || player.HasBuff(BlessingOfMight) || player.HasBuff(BlessingOfKings) || player.HasBuff(BlessingOfSanctuary))
            {
                botStates.Pop();
                return;
            }

            if (player.KnowsSpell(BlessingOfMight) && !player.KnowsSpell(BlessingOfKings) && !player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfMight);

            if (player.KnowsSpell(BlessingOfKings) && !player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfKings);

            if (player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfSanctuary);
        }

        /// <summary>
        /// Tries to cast a spell with the given name if the player does not have the specified buff, the spell is ready, and the player has enough mana.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Method" as T
        /// participant "Player" as P
        /// participant "ClientHelper" as C
        /// 
        /// T -> P: HasBuff(name)
        /// activate P
        /// P --> T: Buff status
        /// deactivate P
        /// 
        /// T -> P: IsSpellReady(name)
        /// activate P
        /// P --> T: Spell readiness status
        /// deactivate P
        /// 
        /// T -> P: GetManaCost(name)
        /// activate P
        /// P --> T: Mana cost
        /// deactivate P
        /// 
        /// T -> C: ClientVersion
        /// activate C
        /// C --> T: Version
        /// deactivate C
        /// 
        /// alt ClientVersion.Vanilla
        ///     T -> P: LuaCall(CastSpellByName)
        /// else
        ///     T -> P: CastSpell(name, Guid)
        /// end
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name)
        {
            if (!player.HasBuff(name) && player.IsSpellReady(name) && player.Mana > player.GetManaCost(name))
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpellByName(\"{name}\",1)");
                }
                else
                {
                    player.CastSpell(name, player.Guid);
                }
            }
        }
    }
}
