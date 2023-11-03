using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

/// <summary>
/// The FuryWarriorBot namespace contains the implementation of a bot for playing as a Fury Warrior character in a game.
/// </summary>
namespace FuryWarriorBot
{
    /// <summary>
    /// Omit any targets that bring the player too close to another threat while moving to that target.
    /// </summary>
    /// <summary>
    /// Omit any targets that bring the player too close to another threat while moving to that target.
    /// </summary>
    [Export(typeof(IBot))]
    class FuryWarriorBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Fury Warrior.
        /// </summary>
        public string Name => "Fury Warrior";

        /// <summary>
        /// Gets the file name of the FuryWarriorBot.dll.
        /// </summary>
        public string FileName => "FuryWarriorBot.dll";

        /// <summary>
        /// Omit any targets that bring the player too close to another threat while moving to that target.
        /// </summary>
        // omit any targets that bring the player too close to another threat while moving to that target
        bool AdditionalTargetingCriteria(WoWUnit u) =>
            !ObjectManager.Units.Any(o =>
                o.Level > ObjectManager.Player.Level - 4 &&
                (o.UnitReaction == UnitReaction.Hated || o.UnitReaction == UnitReaction.Hostile) &&
                o.Guid != ObjectManager.Player.Guid &&
                o.Guid != u.Guid &&
                false &&
                Navigation.CalculatePath(ObjectManager.MapId, ObjectManager.Player.Position, u.Position, false).Any(p => p.DistanceTo(o.Position) < 30) &&
                u.Position.DistanceTo(o.Position) < 30
            );

        /// <summary>
        /// Creates a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
                    new RestState(botStates, container);

        /// <summary>
        /// Creates a new instance of the MoveToTargetState class and returns it.
        /// </summary>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                    new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
                    new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        /// <summary>
        /// Gets the dependency container for the bot.
        /// </summary>
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
        /// This method is used to test the functionality of the dependency container.
        /// </summary>
        public void Test(IDependencyContainer container) { }
    }
}
