// Ported from BloogBot/BackstabRogueBot/PowerlevelCombatState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
// Nat owns this file!

using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace BackstabRogueBot
{
    class PowerlevelCombatState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly WoWUnit target;

        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
        }

        public void Update()
        {
            // TODO
        }
    }
}
