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
    /// This class represents a state where the bot buffs itself.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Power Word: Fortitude".
        /// </summary>
        const string PowerWordFortitude = "Power Word: Fortitude";
        /// <summary>
        /// Represents the constant string "Shadow Protection".
        /// </summary>
        const string ShadowProtection = "Shadow Protection";

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
        /// Updates the player's buffs by casting Power Word Fortitude and Shadow Protection if necessary.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: KnowsSpell(PowerWordFortitude)
        /// Player --> Update: Response
        /// Update -> Player: HasBuff(PowerWordFortitude)
        /// Player --> Update: Response
        /// Update -> Player: KnowsSpell(ShadowProtection)
        /// Player --> Update: Response
        /// Update -> Player: HasBuff(ShadowProtection)
        /// Player --> Update: Response
        /// Update -> BotStates: Pop
        /// Update -> Update: TryCastSpell(PowerWordFortitude)
        /// Update -> Update: TryCastSpell(ShadowProtection)
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if ((!player.KnowsSpell(PowerWordFortitude) || player.HasBuff(PowerWordFortitude)) && (!player.KnowsSpell(ShadowProtection) || player.HasBuff(ShadowProtection)))
            {
                botStates.Pop();
                return;
            }

            TryCastSpell(PowerWordFortitude);

            TryCastSpell(ShadowProtection);
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player has the required level and the spell is ready.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Function" as TCS
        /// participant "Player" as P
        /// TCS -> P: HasBuff(name)
        /// alt player does not have buff and level is sufficient and spell is ready
        ///     TCS -> P: IsSpellReady(name)
        ///     TCS -> P: LuaCall("CastSpellByName('name',1)")
        /// end
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name, int requiredLevel = 1)
        {
            if (!player.HasBuff(name) && player.Level >= requiredLevel && player.IsSpellReady(name))
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
