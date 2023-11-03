using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Arcane Mage bot.
/// </summary>
namespace ArcaneMageBot
{
    /// <summary>
    /// Represents the constant string value for "Dampen Magic".
    /// </summary>
    /// <summary>
    /// Represents the constant string value for "Dampen Magic".
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string value for "Arcane Intellect".
        /// </summary>
        const string ArcaneIntellect = "Arcane Intellect";
        /// <summary>
        /// Represents the constant string "Frost Armor".
        /// </summary>
        const string FrostArmor = "Frost Armor";
        /// <summary>
        /// Represents the constant string "Ice Armor".
        /// </summary>
        const string IceArmor = "Ice Armor";
        /// <summary>
        /// Represents the constant string "Dampen Magic".
        /// </summary>
        const string DampenMagic = "Dampen Magic";

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
        /// Updates the player's buffs and casts necessary spells if conditions are met.
        /// </summary>
        public void Update()
        {
            if ((!player.KnowsSpell(ArcaneIntellect) || player.HasBuff(ArcaneIntellect)) && (player.HasBuff(FrostArmor) || player.HasBuff(IceArmor)) && (!player.KnowsSpell(DampenMagic) || player.HasBuff(DampenMagic)))
            {
                botStates.Pop();
                botStates.Push(new ConjureItemsState(botStates, container));
                return;
            }

            TryCastSpell(ArcaneIntellect, castOnSelf: true);

            if (player.KnowsSpell(IceArmor))
                TryCastSpell(IceArmor);
            else
                TryCastSpell(FrostArmor);

            TryCastSpell(DampenMagic, castOnSelf: true);
        }

        /// <summary>
        /// Tries to cast a spell with the given name. If the player does not have the specified buff, knows the spell, and the spell is ready, it will be casted. 
        /// </summary>
        /// <param name="name">The name of the spell to cast.</param>
        /// <param name="castOnSelf">Optional parameter to specify whether the spell should be casted on self. Default is false.</param>
        void TryCastSpell(string name, bool castOnSelf = false)
        {
            if (!player.HasBuff(name) && player.KnowsSpell(name) && player.IsSpellReady(name))
            {
                var castOnSelfString = castOnSelf ? ",1" : "";
                player.LuaCall($"CastSpellByName('{name}'{castOnSelfString})");
            }
        }
    }
}
