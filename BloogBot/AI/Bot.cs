using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using BloogBot.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BloogBot.AI
{
    public abstract class Bot
    {
        readonly Stack<IBotState> botStates = new Stack<IBotState>();
        readonly Stopwatch stopwatch = new Stopwatch();
        static readonly Random random = new Random();

        bool running;
        bool retrievingCorpse;

        Type currentState;
        int currentStateStartTime;
        Position currentPosition;
        int currentPositionStartTime;
        Position teleportCheckPosition;
        bool isFalling;
        int currentLevel;

        Action stopCallback;

        public bool Running() => running;

        public void Stop()
        {
            running = false;
            currentLevel = 0;

            while (botStates.Count > 0)
                botStates.Pop();

            stopCallback?.Invoke();
        }

        public void Start(IDependencyContainer container, Action stopCallback)
        {
            this.stopCallback = stopCallback;

            try
            {
                running = true;

                ThreadSynchronizer.RunOnMainThread(() =>
                {
                    ResetValues(container);
                });

                container.CheckForTravelPath(botStates, false);
                StartInternal(container);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        private void ResetValues(IDependencyContainer container)
        {
            currentLevel = ObjectManager.Player.Level;
            botStates.Push(new GrindState(botStates, container));
            currentState = botStates.Peek().GetType();
            currentStateStartTime = Environment.TickCount;
            currentPosition = ObjectManager.Player.Position;
            currentPositionStartTime = Environment.TickCount;
            teleportCheckPosition = ObjectManager.Player.Position;
            ObjectManager.Player.CurrWpId = 0;
            ObjectManager.Player.LastWpId = 0;
            ObjectManager.Player.CurrZone = "0";
            ObjectManager.Player.DeathsAtWp = 0;
            ObjectManager.Player.WpStuckCount = 0;
            ObjectManager.Player.ForcedWpPath = new List<int>();
            ObjectManager.Player.VisitedWps = new HashSet<int>();
            ObjectManager.Player.HasBeenStuckAtWp = false;
            ObjectManager.Player.HasJoinedBg = false;
            ObjectManager.Player.HasOverLeveled = false;
            ObjectManager.Player.LastKnownMapId = ObjectManager.MapId;
        }

        public void Travel(IDependencyContainer container, bool reverseTravelPath, Action callback)
        {
            try
            {
                running = true;

                var waypoints = container
                    .BotSettings
                    .CurrentTravelPath
                    .Waypoints;

                if (reverseTravelPath)
                    waypoints = waypoints.Reverse().ToArray();

                var closestWaypoint = waypoints
                    .OrderBy(w => w.DistanceTo(ObjectManager.Player.Position))
                    .First();

                var startingIndex = waypoints
                    .ToList()
                    .IndexOf(closestWaypoint);

                ThreadSynchronizer.RunOnMainThread(() =>
                {
                    currentLevel = ObjectManager.Player.Level;

                    void callbackInternal()
                    {
                        running = false;
                        currentState = null;
                        currentPosition = null;
                        callback();
                    }

                    botStates.Push(new TravelState(botStates, container, waypoints, startingIndex, callbackInternal));
                    botStates.Push(new MoveToPositionState(botStates, container, closestWaypoint));

                    currentState = botStates.Peek().GetType();
                    currentStateStartTime = Environment.TickCount;
                    currentPosition = ObjectManager.Player.Position;
                    currentPositionStartTime = Environment.TickCount;
                    teleportCheckPosition = ObjectManager.Player.Position;
                });

                StartInternal(container);
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        public void StartPowerlevel(IDependencyContainer container, Action stopCallback)
        {
            this.stopCallback = stopCallback;

            try
            {
                running = true;

                ThreadSynchronizer.RunOnMainThread(() =>
                {
                    botStates.Push(new PowerlevelState(botStates, container));

                    currentState = botStates.Peek().GetType();
                    currentStateStartTime = Environment.TickCount;
                    currentPosition = ObjectManager.Player.Position;
                    currentPositionStartTime = Environment.TickCount;
                    teleportCheckPosition = ObjectManager.Player.Position;
                });

                StartPowerlevelInternal(container);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        async void StartPowerlevelInternal(IDependencyContainer container)
        {
            while (running)
            {
                try
                {
                    stopwatch.Restart();

                    ThreadSynchronizer.RunOnMainThread(() =>
                    {
                        if (botStates.Count() == 0)
                        {
                            Stop();
                            return;
                        }

                        var player = ObjectManager.Player;
                        player.AntiAfk();

                        if (player.IsFalling)
                        {
                            container.DisableTeleportChecker = true;
                            isFalling = true;
                        }

                        if (player.Position.DistanceTo(teleportCheckPosition) > 10 && !container.DisableTeleportChecker && container.BotSettings.UseTeleportKillswitch)
                        {
                            DiscordClientWrapper.TeleportAlert(player.Name);
                            Stop();
                            return;
                        }
                        teleportCheckPosition = player.Position;

                        if (isFalling && !player.IsFalling)
                        {
                            container.DisableTeleportChecker = false;
                            isFalling = false;
                        }

                        if (botStates.Count > 0 && botStates.Peek()?.GetType() == typeof(GrindState))
                        {
                            container.RunningErrands = false;
                            retrievingCorpse = false;
                        }

                        // if the player has been stuck in the same state for more than 5 minutes
                        if (Environment.TickCount - currentStateStartTime > 300000 && currentState != typeof(TravelState) && container.BotSettings.UseStuckInStateKillswitch)
                        {
                            var msg = $"Hey, it's {player.Name}, and I need help! I've been stuck in the {currentState.Name} for over 5 minutes. I'm stopping for now.";
                            LogToFile(msg);
                            DiscordClientWrapper.SendMessage(msg);
                            Stop();
                            return;
                        }
                        if (botStates.Peek().GetType() != currentState)
                        {
                            currentState = botStates.Peek().GetType();
                            currentStateStartTime = Environment.TickCount;
                        }

                        // if the player has been stuck in the same position for more than 5 minutes
                        if (Environment.TickCount - currentPositionStartTime > 300000 && container.BotSettings.UseStuckInPositionKillswitch)
                        {
                            var msg = $"Hey, it's {player.Name}, and I need help! I've been stuck in the same position for over 5 minutes. I'm stopping for now.";
                            LogToFile(msg);
                            DiscordClientWrapper.SendMessage(msg);
                            Stop();
                            return;
                        }
                        if (player.Position.DistanceTo(currentPosition) > 10)
                        {
                            currentPosition = player.Position;
                            currentPositionStartTime = Environment.TickCount;
                        }

                        // if the player dies
                        if ((player.Health <= 0 || player.InGhostForm) && !retrievingCorpse)
                        {
                            PopStackToBaseState();

                            retrievingCorpse = true;
                            container.RunningErrands = true;

                            container.DisableTeleportChecker = true;

                            botStates.Push(container.CreateRestState(botStates, container));
                            botStates.Push(new RetrieveCorpseState(botStates, container));
                            botStates.Push(new MoveToCorpseState(botStates, container));
                            botStates.Push(new ReleaseCorpseState(botStates, container));
                        }

                        var currentHotspot = container.GetCurrentHotspot();

                        // if equipment needs to be repaired
                        int mainhandDurability = Inventory.GetEquippedItem(EquipSlot.MainHand)?.DurabilityPercentage ?? 100;
                        int offhandDurability = Inventory.GetEquippedItem(EquipSlot.Ranged)?.DurabilityPercentage ?? 100;

                        // offhand throwns don't have durability, but instead register `-2147483648`.
                        // This is a workaround to prevent that from causing us to get caught in a loop.
                        // We default to a durability value of 100 for items that are null because 100 will register them as not needing repaired.
                        if ((mainhandDurability <= 20 && mainhandDurability > -1 || (offhandDurability <= 20 && offhandDurability > -1)) && currentHotspot.RepairVendor != null && !container.RunningErrands)
                        {
                            ShapeshiftToHumanForm(container);
                            PopStackToBaseState();

                            container.RunningErrands = true;

                            if (currentHotspot.TravelPath != null)
                            {
                                botStates.Push(new TravelState(botStates, container, currentHotspot.TravelPath.Waypoints, 0));
                                botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.TravelPath.Waypoints[0]));
                            }

                            botStates.Push(new RepairEquipmentState(botStates, container, currentHotspot.RepairVendor.Name));
                            botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.RepairVendor.Position));
                            container.CheckForTravelPath(botStates, true);
                        }

                        if (botStates.Count > 0)
                        {
                            container.Probe.CurrentState = botStates.Peek()?.GetType().Name;
                            botStates.Peek()?.Update();
                        }
                    });

                    await Task.Delay(25);

                    container.Probe.UpdateLatency = $"{stopwatch.ElapsedMilliseconds}ms";
                }
                catch (Exception e)
                {
                    Logger.Log(e + "\n");
                }
            }
        }

        async void StartInternal(IDependencyContainer container)
        {
            while (running)
            {
                try
                {
                    stopwatch.Restart();

                    ThreadSynchronizer.RunOnMainThread(() =>
                    {
                        var player = ObjectManager.Player;

                        // BG checks
                        var playerInBg = IsPlayerInBg();
                        if (player.HasJoinedBg && Wait.For("JoinedBGDelay", 30000))
                            player.HasJoinedBg = false;
                        else if (player.HasJoinedBg)
                        {
                            if (!playerInBg)
                                player.LuaCall("StaticPopup1Button1:Click()"); // Try to join again
                            return;
                        }

                        // If in BG, check if it has ended
                        if (playerInBg)
                        {
                            if (IsBgFinished(player) || player.HasLeftBg)
                            {
                                player.LuaCall("LeaveBattlefield()");
                                player.HasLeftBg = true;
                            }
                        }

                        if (player.HasLeftBg && Wait.For("LeftBGDelay", 6000))
                            player.HasLeftBg = false;
                        else if (player.HasLeftBg)
                            return;

                        if (ObjectManager.MapId != player.LastKnownMapId)
                        {
                            Console.WriteLine("Bot entered new map... Restarting bot!");
                            //Stop();
                            ObjectManager.KillswitchTriggered = false;
                            //Start(container, stopCallback);
                            ResetValues(container);
                            player.LastKnownMapId = ObjectManager.MapId;
                        }

                        if (botStates.Count() == 0)
                        {
                            Stop();
                            return;
                        }

                        if (!playerInBg && player.Level > currentLevel)
                        {
                            currentLevel = player.Level;
                            DiscordClientWrapper.SendMessage($"Ding! {player.Name} is now level {player.Level}!");
                        }

                        player.AntiAfk();

                        if (container.UpdatePlayerTrackers())
                        {
                            Stop();
                            return;
                        }

                        if (player.IsFalling)
                        {
                            container.DisableTeleportChecker = true;
                            isFalling = true;
                        }

                        if (player.Position.DistanceTo(teleportCheckPosition) > 5 && !container.DisableTeleportChecker && container.BotSettings.UseTeleportKillswitch)
                        {
                            DiscordClientWrapper.TeleportAlert(player.Name);
                            Stop();
                            return;
                        }
                        teleportCheckPosition = player.Position;

                        if (isFalling && !player.IsFalling)
                        {
                            container.DisableTeleportChecker = false;
                            isFalling = false;
                        }

                        if (botStates.Count > 0 && (botStates.Peek()?.GetType() == typeof(GrindState) || botStates.Peek()?.GetType() == typeof(PowerlevelState)))
                        {
                            container.RunningErrands = false;
                            retrievingCorpse = false;
                        }

                        var HasBeenForcedToTeleport = false;
                        // if the player has been stuck in combat for more than 3 minutes
                        if (Environment.TickCount - currentStateStartTime > 180000 && currentState.IsSubclassOf(typeof(CombatStateBase)))
                        {
                            // Blacklist current target
                            var target = player.Target;
                            if (target != null)
                            {
                                if (Repository.BlacklistedMobExists(target.Guid))
                                    Console.WriteLine("Error: Tried to blacklist target that's already blacklisted");
                                else
                                {
                                    Repository.AddBlacklistedMob(target.Guid);
                                    container.Probe.BlacklistedMobIds.Add(target.Guid);
                                }
                            }

                            // Force teleport to current WP
                            if (player.CurrWpId == 0)
                                player.CurrWpId = container.GetCurrentHotspot().Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault().ID;
                            if (random.Next(0, 2) == 0)
                                player.CurrWpId = container.GetCurrentHotspot().Waypoints.OrderBy(w => player.Position.DistanceTo(w)).ElementAtOrDefault(1).ID;
                            Console.WriteLine($"Forcing teleport to WP: {player.CurrWpId} (Stuck in combat state - target blacklisted)!");
                            player.LuaCall($"SendChatMessage('.npcb wp go {player.CurrWpId}')");
                            player.SetTarget(player.Guid);
                            botStates.Pop();
                            botStates.Push(new GrindState(botStates, container));
                            HasBeenForcedToTeleport = true;
                        }

                        // if the player has been stuck in the same state for more than 7 minutes
                        if (Environment.TickCount - currentStateStartTime > 420000 && currentState != typeof(TravelState) && container.BotSettings.UseStuckInStateKillswitch)
                        {
                            HandleBotStuck(container, player, true);
                            //var msg = $"Hey, it's {player.Name}, and I need help! I've been stuck in the {currentState.Name} for over 5 minutes. I'm stopping for now.";
                            //LogToFile(msg);
                            //DiscordClientWrapper.SendMessage(msg);
                            //Stop();
                            //return;
                        }
                        if (botStates.Peek().GetType() != currentState)
                        {
                            currentState = botStates.Peek().GetType();
                            currentStateStartTime = Environment.TickCount;
                        }

                        // if the player has been stuck in the same position for more than 7 minutes
                        //if (Environment.TickCount - currentPositionStartTime >  420000 && container.BotSettings.UseStuckInPositionKillswitch)
                        if (!HasBeenForcedToTeleport && Environment.TickCount - currentPositionStartTime > 420000 && container.BotSettings.UseStuckInPositionKillswitch)
                        {
                            HandleBotStuck(container, player, false);
                            //var msg = $"Hey, it's {player.Name}, and I need help! I've been stuck in the same position for over 5 minutes. I'm stopping for now.";
                            //LogToFile(msg);
                            //DiscordClientWrapper.SendMessage(msg);
                            //Stop();
                            //return;
                        }

                        if (player.Position.DistanceTo(currentPosition) > 10)
                        {
                            currentPosition = player.Position;
                            currentPositionStartTime = Environment.TickCount;
                        }

                        // if the player dies
                        if ((player.Health <= 0 || player.InGhostForm) && !retrievingCorpse)
                        {
                            player.DeathsAtWp++;
                            Console.WriteLine($"Player died. DeathsAtWp: {player.DeathsAtWp}");
                            player.ForcedWpPath = new List<int>();
                            PopStackToBaseState();

                            retrievingCorpse = true;
                            container.RunningErrands = true;

                            container.DisableTeleportChecker = true;
                            player.CurrWpId = 0;
                            player.WpStuckCount = 0;

                            botStates.Push(container.CreateRestState(botStates, container));
                            if (playerInBg)
                                botStates.Push(new ReleaseCorpseState(botStates, container));
                            else
                            {
                                botStates.Push(new RetrieveCorpseState(botStates, container));
                                botStates.Push(new MoveToCorpseState(botStates, container));
                                botStates.Push(new ReleaseCorpseState(botStates, container));
                            }
                        }

                        if (!playerInBg)
                        {
                            var currentHotspot = container.GetCurrentHotspot();

                            // if equipment needs to be repaired
                            int mainhandDurability = Inventory.GetEquippedItem(EquipSlot.MainHand)?.DurabilityPercentage ?? 100;
                            int offhandDurability = Inventory.GetEquippedItem(EquipSlot.Ranged)?.DurabilityPercentage ?? 100;
                            // Also check legs since weapon might be heirloom
                            int legsDurability = Inventory.GetEquippedItem(EquipSlot.Legs)?.DurabilityPercentage ?? 100;

                            // offhand throwns don't have durability, but instead register `-2147483648`.
                            // This is a workaround to prevent that from causing us to get caught in a loop.
                            // We default to a durability value of 100 for items that are null because 100 will register them as not needing repaired.
                            if ((legsDurability <= 10 || (mainhandDurability <= 20 && mainhandDurability > -1 || (offhandDurability <= 20 && offhandDurability > -1))) && currentHotspot.RepairVendor != null && !container.RunningErrands)
                            {
                                ShapeshiftToHumanForm(container);
                                PopStackToBaseState();

                                Console.WriteLine($"Bot needs to repair!");
                                if (currentHotspot.Id == 1)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 31')"); // Kalimdor horde repair
                                else if (currentHotspot.Id == 2)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 34')"); // Kalimdor alliance repair
                                else if (currentHotspot.Id == 3)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 4')"); // EK horde repair
                                else if (currentHotspot.Id == 4)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 13')"); // EK alliance repair
                                else if (currentHotspot.Id == 5)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 2578')"); // Outland horde repair
                                else if (currentHotspot.Id == 6)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 2601')"); // Outland alliance repair
                                else if (currentHotspot.Id == 7)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 2730')"); // Northrend horde repair
                                else if (currentHotspot.Id == 8)
                                    player.LuaCall($"SendChatMessage('.npcb wp go 2703')"); // Northrend alliance repair

                                player.CurrWpId = 0;
                                player.WpStuckCount = 0;

                                container.RunningErrands = true;

                                if (currentHotspot.TravelPath != null)
                                {
                                    botStates.Push(new TravelState(botStates, container, currentHotspot.TravelPath.Waypoints, 0));
                                    botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.TravelPath.Waypoints[0]));
                                }

                                botStates.Push(new RepairEquipmentState(botStates, container, currentHotspot.RepairVendor.Name));
                                botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.RepairVendor.Position));
                                container.CheckForTravelPath(botStates, true);
                            }

                            // if inventory is full
                            //if (Inventory.CountFreeSlots(false) == 0 && currentHotspot.Innkeeper != null && !container.RunningErrands)
                            //{
                            //    ShapeshiftToHumanForm(container);
                            //    PopStackToBaseState();

                            //    container.RunningErrands = true;

                            //    if (currentHotspot.TravelPath != null)
                            //    {
                            //        botStates.Push(new TravelState(botStates, container, currentHotspot.TravelPath.Waypoints, 0));
                            //        botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.TravelPath.Waypoints[0]));
                            //    }
                            //    
                            //    botStates.Push(new SellItemsState(botStates, container, currentHotspot.Innkeeper.Name));
                            //    botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.Innkeeper.Position));
                            //    container.CheckForTravelPath(botStates, true);
                            //}
                        }

                        if (botStates.Count > 0)
                        {
                            container.Probe.CurrentState = botStates.Peek()?.GetType().Name;
                            botStates.Peek()?.Update();
                        }
                        else
                            Console.WriteLine("Bot states empty...");
                    });

                    await Task.Delay(100);

                    container.Probe.UpdateLatency = $"{stopwatch.ElapsedMilliseconds}ms";
                }
                catch (Exception e)
                {
                    Logger.Log(e + "\n");
                }
            }
            Console.WriteLine("End of loop");
        }

        private void HandleBotStuck(IDependencyContainer container, LocalPlayer player, bool StuckInState)
        {
            // Force teleport to current WP, or to corpse if dead
            if (player.Health <= 0 || player.InGhostForm)
            {
                // Force teleport to corpse
                Console.WriteLine($"Forcing teleport to corpse " + (StuckInState ? "(UseStuckInStateKillswitch)" : "(UseStuckInPositionKillswitch)"));
                player.LuaCall($"SendChatMessage('.go xyz {player.CorpsePosition.X.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Y.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Z.ToString().Replace(',', '.')}', 'GUILD', nil)");
            }
            else
            {
                if (player.CurrWpId == 0)
                    player.CurrWpId = container.GetCurrentHotspot().Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault().ID;
                Console.WriteLine($"Forcing teleport to WP: {player.CurrWpId} " + (StuckInState ? "(UseStuckInStateKillswitch)" : "(UseStuckInPositionKillswitch)"));
                player.LuaCall($"SendChatMessage('.npcb wp go {player.CurrWpId}')");
                botStates.Pop();
                botStates.Push(new GrindState(botStates, container));
            }
        }

        private bool IsPlayerInBg()
        {
            var mapId = ObjectManager.MapId;
            return (mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559);
        }

        private bool IsBgFinished(LocalPlayer player)
        {
            var result = player.LuaCallWithResults($"{{0}} = GetBattlefieldWinner()");

            if (result.Length > 0)
                return result[0] == "0" || result[0] == "1";
            else
                return false;
        }

        private void LogToFile(string text)
        {
            var dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(MainViewModel)).CodeBase);
            var path = new UriBuilder(dir).Path;
            var file = Path.Combine(path, "StuckLog.txt");

            if (File.Exists(file))
            {
                using (var sw = File.AppendText(file))
                {
                    sw.WriteLine(text);
                }
            }
        }

        void ShapeshiftToHumanForm(IDependencyContainer container)
        {
            if (ObjectManager.Player.Class == Class.Druid && ObjectManager.Player.CurrentShapeshiftForm == "Bear Form")
                ObjectManager.Player.LuaCall("CastSpellByName('Bear Form')");
            if (ObjectManager.Player.Class == Class.Druid && ObjectManager.Player.CurrentShapeshiftForm == "Cat Form")
                ObjectManager.Player.LuaCall("CastSpellByName('Cat Form')");
        }

        void PopStackToBaseState()
        {
            while (botStates.Count > 1)
                botStates.Pop();
        }
    }
}
