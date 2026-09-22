// Ported from BloogBot/BeastmasterHunterBot/BeastMasterHunterBot.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
// Friday owns this file!

using BeastmasterHunterBot;
using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace BeastMasterHunterBot
{
    [Export(typeof(IBot))]
    class BeastMasterHunterBot : Bot, IBot
    {
        public string Name => "Beast Master Hunter";

        // POTENTIAL BUG FOUND: FileName spells the assembly 'BeastMasterHunterBot.dll' but the
        //   project builds 'BeastmasterHunterBot.dll' (lowercase 'm'), so the !info command's
        //   AssemblyName.GetAssemblyName($"{path}\\{CurrentBot.FileName}") throws
        //   FileNotFoundException on a case-sensitive volume. Same mismatch as the
        //   BeastMasterHunterBot.dll entry in BloogBot/BotLoader.cs:40; it only works today
        //   because NTFS is case-insensitive.
        //   Original: BloogBot/BeastmasterHunterBot/BeastMasterHunterBot.cs:18
        //   Ported as-is - behavior matches .NET Framework BloogBot.
        public string FileName => "BeastMasterHunterBot.dll";

        bool AdditionalTargetingCriteria(WoWUnit unit) => true;

        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
            new RestState(botStates, container);

        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
            new MoveToTargetState(botStates, container, target);

        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
            new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        IBotState CreateCombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) => new CombatState(botStates, container, target, loot);

        public IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots) =>
            new DependencyContainer(
                AdditionalTargetingCriteria,
                CreateRestState,
                CreateMoveToTargetState,
                CreatePowerlevelCombatState,
                CreateCombatState,
                botSettings,
                probe,
                hotspots);

        public void Test(IDependencyContainer container)
        {
            var player = ObjectManager.Player;
            player.LuaCall("StartAttack()");
        }
    }
}
