using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Frost Mage bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents the constant string value for "Ice Armor".
    /// </summary>
    /// <summary>
    /// Represents the constant string value for "Ice Armor".
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string value for "Arcane Intellect".
        /// </summary>
        const string ArcaneIntellect = "Arcane Intellect";
        /// <summary>
        /// Represents the constant string "Dampen Magic".
        /// </summary>
        const string DampenMagic = "Dampen Magic";
        /// <summary>
        /// Represents the constant string "Frost Armor".
        /// </summary>
        const string FrostArmor = "Frost Armor";
        /// <summary>
        /// Represents the constant string "Ice Armor".
        /// </summary>
        const string IceArmor = "Ice Armor";
        /// <summary>
        /// Represents the constant string "Mage Armor".
        /// </summary>
        const string MageArmor = "Mage Armor";

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
        /// Initializes a new instance of the <see cref="BuffSelfState"/> class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's buffs and casts necessary spells.
        /// If the player does not know the spell Arcane Intellect or has the buff Arcane Intellect,
        /// and has either the Frost Armor, Ice Armor, or Mage Armor buff,
        /// and does not know the spell Dampen Magic or has the buff Dampen Magic,
        /// the bot state is changed to ConjureItemsState and returns.
        /// Otherwise, the spell Arcane Intellect is cast on self.
        /// If the player knows the spell Mage Armor, the spell Mage Armor is cast.
        /// If the player knows the spell Ice Armor, the spell Ice Armor is cast.
        /// Otherwise, the spell Frost Armor is cast.
        /// Finally, the spell Dampen Magic is cast on self.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> player: KnowsSpell(ArcaneIntellect)
        /// Update -> player: HasBuff(ArcaneIntellect)
        /// Update -> player: HasBuff(FrostArmor)
        /// Update -> player: HasBuff(IceArmor)
        /// Update -> player: HasBuff(MageArmor)
        /// Update -> player: KnowsSpell(DampenMagic)
        /// Update -> player: HasBuff(DampenMagic)
        /// Update -> botStates: Pop()
        /// Update -> botStates: Push(new ConjureItemsState)
        /// Update -> TryCastSpell: ArcaneIntellect, castOnSelf: true
        /// Update -> player: KnowsSpell(MageArmor)
        /// Update -> TryCastSpell: MageArmor
        /// Update -> player: KnowsSpell(IceArmor)
        /// Update -> TryCastSpell: IceArmor
        /// Update -> TryCastSpell: FrostArmor
        /// Update -> TryCastSpell: DampenMagic, castOnSelf: true
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if ((!player.KnowsSpell(ArcaneIntellect) || player.HasBuff(ArcaneIntellect)) && (player.HasBuff(FrostArmor) || player.HasBuff(IceArmor) || player.HasBuff(MageArmor)) && (!player.KnowsSpell(DampenMagic) || player.HasBuff(DampenMagic)))
            {
                botStates.Pop();
                botStates.Push(new ConjureItemsState(botStates, container));
                return;
            }

            TryCastSpell(ArcaneIntellect, castOnSelf: true);

            if (player.KnowsSpell(MageArmor))
                TryCastSpell(MageArmor);
            else if (player.KnowsSpell(IceArmor))
                TryCastSpell(IceArmor);
            else
                TryCastSpell(FrostArmor);

            TryCastSpell(DampenMagic, castOnSelf: true);
        }

        /// <summary>
        /// Tries to cast a spell with the given name.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Method" as T
        /// participant "Player" as P
        /// participant "ClientHelper" as C
        /// 
        /// T -> P: HasBuff(name)
        /// T -> P: KnowsSpell(name)
        /// T -> P: IsSpellReady(name)
        /// 
        /// alt castOnSelf is true
        ///     T -> C: ClientVersion
        ///     alt ClientVersion is Vanilla
        ///         T -> P: LuaCall(CastSpellByName(name,1))
        ///     else ClientVersion is not Vanilla
        ///         T -> P: CastSpell(name, player.Guid)
        ///     end
        /// else castOnSelf is false
        ///     T -> P: LuaCall(CastSpellByName(name))
        /// end
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name, bool castOnSelf = false)
        {
            if (!player.HasBuff(name) && player.KnowsSpell(name) && player.IsSpellReady(name))
            {
                if (castOnSelf)
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
                else
                    player.LuaCall($"CastSpellByName('{name}')");
            }
        }
    }
}
