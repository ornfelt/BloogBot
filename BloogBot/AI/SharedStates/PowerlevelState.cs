using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Contains the implementation of the PowerlevelState class, which represents the state of the bot when it is in powerleveling mode.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents the state of the power level of the bot.
    /// </summary>
    /// <summary>
    /// Represents the state of the power level of the bot.
    /// </summary>
    class PowerlevelState : IBotState
    {
        /// <summary>
        /// Array of player emotes.
        /// </summary>
        static readonly string[] playerEmotes = { "amaze", "applaud", "bark", "beckon", "belch", "blink", "blow", "blush", "bonk", "bounce", "bow",
            "bravo", "cheer", "chew", "chicken", "chuckle", "clap", "comfort", "cuddle", "curtsey", "dance", "drool", "excited", "fidget",
            "flex", "followme", "gaze", "guffaw", "happy", "highfive", "hug", "impatient", "kiss", "knuckles", "laugh", "listen", "love",
            "massage", "moan", "moo", "nosepick", "pat", "pizza", "ponder", "purr", "roar", "salute", "sexy", "shimmy", "silly", "train",
            "smile", "smirk", "snicker", "sniff", "soothe", "stare", "tickle", "whistle", "wave" };

        /// <summary>
        /// Array of target emotes.
        /// </summary>
        static readonly string[] targetEmotes = { "angry", "bark", "beckon", "bite", "cackle", "flex", "glare", "plead", "roar", "rude",
            "scowl", "openfire", "slap", "snarl", "snicker", "spit", "taunt", "tease", "threaten", "yawn" };

        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

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
        /// Represents a World of Warcraft player.
        /// </summary>
        WoWPlayer targetPlayer;

        /// <summary>
        /// Represents the range of values for an integer.
        /// </summary>
        int range;
        /// <summary>
        /// Represents a boolean value indicating whether the process has started.
        /// </summary>
        bool started;
        /// <summary>
        /// Represents the start time.
        /// </summary>
        int startTime;
        /// <summary>
        /// Represents the start delay for a process.
        /// </summary>
        int startDelay;
        /// <summary>
        /// Represents the distance at which the player should stop.
        /// </summary>
        int playerStopDistance;
        /// <summary>
        /// Represents the search frequency.
        /// </summary>
        double searchFrequency;
        /// <summary>
        /// Represents the frequency of jumps as a double value.
        /// </summary>
        double jumpFrequency;
        /// <summary>
        /// Represents a boolean value indicating whether the object has been initialized.
        /// </summary>
        bool initialized;

        /// <summary>
        /// Initializes a new instance of the PowerlevelState class.
        /// </summary>
        public PowerlevelState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the behavior of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Random: Next(3, 15)
        /// Random --> Update: playerStopDistance
        /// Update -> Random: NextDouble() * 0.05
        /// Random --> Update: searchFrequency
        /// Update -> Random: NextDouble() * 0.005
        /// Random --> Update: jumpFrequency
        /// Update -> ObjectManager: GetTalentRank(3, 11)
        /// ObjectManager --> Update: range
        /// Update -> Random: NextDouble()
        /// Random --> Update: Jump() decision
        /// Update -> Player: LuaCall("Jump()")
        /// Update -> Random: NextDouble()
        /// Random --> Update: Emote decision
        /// Update -> Update: Emote()
        /// Update -> ObjectManager: Players.FirstOrDefault()
        /// ObjectManager --> Update: targetPlayer
        /// Update -> ObjectManager: Units.FirstOrDefault()
        /// ObjectManager --> Update: target
        /// Update -> Player: SetTarget(targetPlayer.Guid)
        /// Update -> Random: Next(200, 2000)
        /// Random --> Update: startDelay
        /// Update -> Player: Position.DistanceTo(target.Position)
        /// Player --> Update: distance
        /// Update -> Player: SetTarget(target.Guid)
        /// Update -> Navigation: GetNextWaypoint()
        /// Navigation --> Update: nextWaypoint
        /// Update -> Player: MoveToward(nextWaypoint)
        /// Update -> Player: Position.DistanceTo(targetPlayer.Position)
        /// Player --> Update: distance
        /// Update -> Navigation: GetNextWaypoint()
        /// Navigation --> Update: nextWaypoint
        /// Update -> Player: MoveToward(nextWaypoint)
        /// Update -> Player: StopAllMovement()
        /// Update -> Random: NextDouble()
        /// Random --> Update: newFacing decision
        /// Update -> Player: SetFacing(newFacing)
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (!initialized)
            {
                playerStopDistance = random.Next(3, 15);
                searchFrequency = random.NextDouble() * 0.05;
                jumpFrequency = random.NextDouble() * 0.005;
                range = 30 + (ObjectManager.GetTalentRank(3, 11) * 3);
                initialized = true;
            }

            if (random.NextDouble() < jumpFrequency)
                player.LuaCall("Jump()");

            if (random.NextDouble() < 0.0025)
                Emote();

            targetPlayer = ObjectManager.Players.FirstOrDefault(p => p.Name == container.BotSettings.PowerlevelPlayerName);
            if (targetPlayer != null)
            {
                var target = ObjectManager.Units
                    .FirstOrDefault(u =>
                        u.Guid == targetPlayer.TargetGuid
                        && u.HealthPercent > 0
                        && (u.UnitReaction == UnitReaction.Hostile || u.UnitReaction == UnitReaction.Neutral)
                    )
                    ?? ObjectManager.Aggressors.FirstOrDefault();

                if (target == null || (player.TargetGuid != target.Guid && player.TargetGuid != targetPlayer.Guid))
                    player.SetTarget(targetPlayer.Guid);

                if (target != null)
                {
                    if (!started)
                    {
                        startTime = Environment.TickCount;
                        startDelay = random.Next(200, 2000);
                        started = true;
                    }

                    if (!started || Environment.TickCount - startTime < startDelay)
                        return;

                    if (player.Position.DistanceTo(target.Position) > range)
                    {
                        if (player.TargetGuid != target.Guid)
                            player.SetTarget(target.Guid);

                        var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                        player.MoveToward(nextWaypoint);
                    }
                    else
                    {
                        initialized = false;
                        started = false;
                        botStates.Push(container.CreatePowerlevelCombatState(botStates, container, target, targetPlayer));
                        return;
                    }
                }
                else
                {
                    if (player.Position.DistanceTo(targetPlayer.Position) > playerStopDistance)
                    {
                        var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, targetPlayer.Position, false);
                        player.MoveToward(nextWaypoint);
                    }
                    else
                    {
                        player.StopAllMovement();

                        if (random.NextDouble() < searchFrequency)
                        {
                            var newFacing = (random.NextDouble() * (Math.PI * 2)) - 0.2;
                            player.SetFacing((float)newFacing);
                        }
                    }
                }
            }
            else
            {
                // TODO: what do we do when the player can't be found (they're too far away, whatever)
            }
        }

        /// <summary>
        /// Performs an emote action based on the current target player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "player" as P
        /// participant "targetPlayer" as TP
        /// 
        /// P -> P: TargetGuid == Guid
        /// alt true
        ///     P -> P: Select emote from playerEmotes
        /// else false
        ///     P -> P: Select emote from targetEmotes
        /// end
        /// P -> P: LuaCall(DoEmote)
        /// \enduml
        /// </remarks>
        void Emote()
        {
            string emote;
            if (player.TargetGuid == targetPlayer.Guid)
                emote = playerEmotes[random.Next(0, playerEmotes.Length - 1)];
            else
                emote = targetEmotes[random.Next(0, targetEmotes.Length - 1)];

            player.LuaCall($"DoEmote('{emote}')");
        }
    }
}
