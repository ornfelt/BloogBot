using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

/// <summary>
/// Namespace for the Arms Warrior Bot.
/// </summary>
namespace ArmsWarriorBot
{
    /// <summary>
    /// Omit any targets that bring the player too close to another threat while moving to that target.
    /// </summary>
    /// <summary>
    /// Omit any targets that bring the player too close to another threat while moving to that target.
    /// </summary>
    [Export(typeof(IBot))]
    class ArmsWarriorBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Arms Warrior.
        /// </summary>
        public string Name => "Arms Warrior";

        /// <summary>
        /// Gets the file name of the ArmsWarriorBot.dll.
        /// </summary>
        public string FileName => "ArmsWarriorBot.dll";

        /// <summary>
        /// Omit any targets that bring the player too close to another threat while moving to that target.
        /// </summary>
        // omit any targets that bring the player too close to another threat while moving to that target
        /// <remarks>
        /// \startuml
        /// ObjectManager -> WoWUnit: Get Units
        /// ObjectManager -> Player: Get Player Level
        /// ObjectManager -> Player: Get Player Guid
        /// ObjectManager -> Player: Get Player Position
        /// ObjectManager -> WoWUnit: Get Unit Level
        /// ObjectManager -> WoWUnit: Get Unit Reaction
        /// ObjectManager -> WoWUnit: Get Unit Guid
        /// ObjectManager -> WoWUnit: Get Unit Position
        /// ObjectManager -> Navigation: Calculate Path
        /// Navigation --> ObjectManager: Return Path
        /// ObjectManager --> WoWUnit: Return AdditionalTargetingCriteria
        /// \enduml
        /// </remarks>
        bool AdditionalTargetingCriteria(WoWUnit u) =>
                    !ObjectManager.Units.Any(o =>
                        o.Level > ObjectManager.Player.Level - 3 &&
                        (o.UnitReaction == UnitReaction.Hated || o.UnitReaction == UnitReaction.Hostile) &&
                        o.Guid != ObjectManager.Player.Guid &&
                        o.Guid != u.Guid &&
                        false &&
                        Navigation.CalculatePath(ObjectManager.MapId, ObjectManager.Player.Position, u.Position, false).Any(p => p.DistanceTo(o.Position) < 30)
                    );

        /// <summary>
        /// Creates a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreateRestState Method" as CRM
        /// participant "RestState" as RS
        /// CRM -> RS: new RestState(botStates, container)
        /// \enduml
        /// </remarks>
        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
                            new RestState(botStates, container);

        /// <summary>
        /// Creates a new instance of the MoveToTargetState class and returns it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreateMoveToTargetState()" as CMTS
        /// participant "MoveToTargetState" as MTS
        /// CMTS -> MTS: new MoveToTargetState(botStates, container, target)
        /// \enduml
        /// </remarks>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                            new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CreatePowerlevelCombatState()" as A
        /// participant "PowerlevelCombatState" as B
        /// A -> B: new PowerlevelCombatState(botStates, container, target, powerlevelTarget)
        /// \enduml
        /// </remarks>
        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
                            new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        /// <summary>
        /// Gets the dependency container for the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "BotSettings" as A
        /// participant "Probe" as B
        /// participant "Hotspot" as C
        /// participant "DependencyContainer" as D
        /// A -> D: botSettings
        /// B -> D: probe
        /// C -> D: hotspots
        /// \enduml
        /// </remarks>
        public IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots) =>
                            new DependencyContainer(
                                AdditionalTargetingCriteria,
                                CreateRestState,
                                CreateMoveToTargetState,
                                CreatePowerlevelCombatState,
                                botSettings,
                                probe,
                                hotspots);

        /// <summary>
        /// Executes a test using the provided dependency container.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Test -> IDependencyContainer: container
        /// activate Test
        /// Test -> ThreadSynchronizer: RunOnMainThread
        /// activate ThreadSynchronizer
        /// ThreadSynchronizer -> Functions: LuaCallWithResult("GetGossipOptions")
        /// activate Functions
        /// Functions --> ThreadSynchronizer: results
        /// deactivate Functions
        /// ThreadSynchronizer -> Console: WriteLine(results[0])
        /// ThreadSynchronizer -> Console: WriteLine(results[1])
        /// ThreadSynchronizer -> Console: WriteLine(results[2])
        /// ThreadSynchronizer -> Console: WriteLine(results[3])
        /// deactivate ThreadSynchronizer
        /// deactivate Test
        /// \enduml
        /// </remarks>
        public void Test(IDependencyContainer container)
        {
            //var target = ObjectManager.Units.FirstOrDefault(u => u.Guid == ObjectManager.Player.TargetGuid);

            //if (target != null)
            //{
            //    Console.WriteLine(target.CanBeLooted);
            //}

            ThreadSynchronizer.RunOnMainThread(() =>
            {
                var results = Functions.LuaCallWithResult($"{{0}}, {{1}}, {{2}}, {{3}} = GetGossipOptions()");
                if (results.Length > 0)
                {
                    Console.WriteLine(results[0]);
                    Console.WriteLine(results[1]);
                    Console.WriteLine(results[2]);
                    Console.WriteLine(results[3]);
                }
            });
        }
    }
}
