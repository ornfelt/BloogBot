using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using BloogBot.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

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
                    ResetValues(container, true);
                });

                container.CheckForTravelPath(botStates, false);
                StartInternal(container);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        private void ResetValues(IDependencyContainer container, bool resetLevel)
        {
            var player = ObjectManager.Player;
            if (resetLevel)
            {
                // First time starting bot
                currentLevel = player.Level;
                // Log datetime to file to separate new bot sessions
                LogToFile("VisitedWanderNodes.txt", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                player.LastWpId = 0;
                // Set faction
                var result = player.LuaCallWithResults($"{{0}} = UnitRace('player')");
                player.IsAlly = result.Length > 0 && new[] { "Human", "Gnome", "Dwarf", "Night Elf", "Draenei" }.Contains(result[0]);
                Console.WriteLine($"Player faction set to: {(player.IsAlly ? "ally" : "horde")}");
                player.BotFriend = player.IsAlly ? "Zalduun" : "Lazarus";
            }
            botStates.Push(new GrindState(botStates, container));
            currentState = botStates.Peek().GetType();
            currentStateStartTime = Environment.TickCount;
            currentPosition = player.Position;
            currentPositionStartTime = Environment.TickCount;
            teleportCheckPosition = player.Position;
            player.CurrWpId = 0;
            player.CurrZone = "0";
            player.DeathsAtWp = 0;
            player.WpStuckCount = 0;
            player.StuckInStateOrPosCount = 0;
            player.ForcedWpPath = new List<int>();
            player.VisitedWps = new HashSet<int>();
            player.HasBeenStuckAtWp = false;
            player.HasJoinedBg = false;
            player.HasOverLeveled = false;
            player.LastKnownMapId = ObjectManager.MapId;
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
                        // Short delay
                        if (player.ShouldWaitForShortDelay && Wait.For("ShortDelay", 600))
                        {
                            player.ShouldWaitForShortDelay = false;
                            HandleLevelUp(player, true); // Try again just in case
                        }
                        else if (player.ShouldWaitForShortDelay)
                            return;

                        // BG / new map delays
                        if (player.HasJoinedBg && Wait.For("JoinedBGDelay", 30000))
                            player.HasJoinedBg = false;
                        else if (player.HasJoinedBg)
                            return;

                        if (player.HasEnteredNewMap && Wait.For("EnteredNewMapDelay", 16000))
                            player.HasEnteredNewMap = false;
                        else if (player.HasEnteredNewMap)
                            return;

                        if (player.ShouldWaitForTeleportDelay && Wait.For("TeleportDelay", 5000))
                            player.ShouldWaitForTeleportDelay = false;
                        else if (player.ShouldWaitForTeleportDelay)
                            return;

                        if (player.ShouldTeleportToLastWp)
                        {
                            player.LuaCall($"SendChatMessage('.npcb wp go {player.LastWpId}')");
                            player.ShouldTeleportToLastWp = false;
                            player.ShouldWaitForTeleportDelay = true;
                            return;
                        }

                        var playerInBg = IsPlayerInBg();
                        // If in BG, check if it has ended
                        if (playerInBg)
                        {
                            if (IsBgFinished(player))
                            {
                                if (ObjectManager.MapId == 559)
                                    player.ShouldTeleportToLastWp = true;
                                player.LuaCall("LeaveBattlefield()");
                                player.LuaCallWithResults("LeaveBattlefield()");
                                player.HasEnteredNewMap = true;
                                return;
                            }
                        }

                        var mapId = ObjectManager.MapId;
                        if ((currentState != typeof(ArenaSkirmishQueueState) || mapId == 559) && mapId != player.LastKnownMapId)
                        {
                            Console.WriteLine("Bot entered new map... Restarting bot!");
                            //Stop();
                            ObjectManager.KillswitchTriggered = false;
                            //Start(container, stopCallback);
                            ResetValues(container, false);
                            player.LastKnownMapId = mapId;
                        }

                        if (botStates.Count() == 0)
                        {
                            Stop();
                            return;
                        }

                        if (!player.IsInCombat && player.HasItemsToEquip)
                        {
                            if (player.LevelItemsDict.TryGetValue(player.Level, out List<int> itemIds))
                                player.LuaCall(string.Join(" ", itemIds.Select(id => $"EquipItemByName({id}); StaticPopup1Button1:Click();")));
                            player.HasItemsToEquip = false;
                        }

                        if (!playerInBg && !player.IsInCombat && player.Level > currentLevel)
                        {
                            currentLevel = player.Level;
                            DiscordClientWrapper.SendMessage($"Ding! {player.Name} is now level {player.Level}!");
                            Console.WriteLine($"Ding! {player.Name} is now level {player.Level}!");
                            //PrintAndLog($"Ding! {player.Name} is now level {player.Level}!");
                            HandleLevelUp(player, false);

                            var shouldSendMail = false;
                            if (shouldSendMail)
                            {
                                // Send mail to notify about levelup
                                var fromAddress = new MailAddress("from_mail", "from_name");
                                var toAddress = new MailAddress("to_mail", "to_name");
                                const string fromPassword = "pass";
                                const string subject = "Bot leveled up!";
                                string body = $"Bot {player.Name} reached level {currentLevel}\nCurrent WP: " +
                                    $"{(player.CurrWpId == 0 ? "0" : container.GetCurrentHotspot().Waypoints.Where(h => h.ID == player.CurrWpId).FirstOrDefault().ToStringFull())}";

                                var smtp = new SmtpClient
                                {
                                    Host = "smtp.gmail.com", // For Gmail, use "smtp.gmail.com"
                                    Port = 587, // For SSL: 465, For TLS/STARTTLS: 587
                                    EnableSsl = true,
                                    DeliveryMethod = SmtpDeliveryMethod.Network,
                                    UseDefaultCredentials = false,
                                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                                };

                                using (var message = new MailMessage(fromAddress, toAddress)
                                {
                                    Subject = subject,
                                    Body = body
                                })
                                {
                                    smtp.Send(message);
                                }
                            }
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
                                player.BlackListedTargets.Add(target.Guid);

                            // Force teleport to current WP
                            ForceTeleport(container, player, $"Stuck in combat state - target: {(target == null ? "" : target.Name)} blacklisted)!");
                            player.SetTarget(player.Guid);
                            botStates.Pop();
                            botStates.Push(new GrindState(botStates, container));
                            HasBeenForcedToTeleport = true;
                        }

                        // if the player has been stuck in the same state for more than 5 minutes
                        if ((Environment.TickCount - currentStateStartTime > 300000 && currentState != typeof(TravelState) && container.BotSettings.UseStuckInStateKillswitch) || player.WpStuckCount > 200)
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

                        // if the player has been stuck in the same position for more than 5 minutes
                        //if (Environment.TickCount - currentPositionStartTime >  420000 && container.BotSettings.UseStuckInPositionKillswitch)
                        if (!HasBeenForcedToTeleport && Environment.TickCount - currentPositionStartTime > 300000 && container.BotSettings.UseStuckInPositionKillswitch)
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
                            {
                                if (ObjectManager.MapId != 559)
                                    botStates.Push(new ReleaseCorpseState(botStates, container));
                            }
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
                                player.LuaCall($"SendChatMessage('{player.HotspotRepairDict[currentHotspot.Id]}')");

                                player.CurrWpId = 0;
                                player.WpStuckCount = 0;
                                player.VisitedWps = new HashSet<int>();

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

        private void HandleLevelUp(LocalPlayer player, bool isSecondTry)
        {
            var playerLevel = player.Level;
            // Handle equipment
            if (!isSecondTry && player.LevelItemsDict.TryGetValue(playerLevel, out List<int> itemIds))
            {
                player.LuaCall(string.Join(" ", itemIds.Select(id => $"SendChatMessage('.additem {id}');")));
                player.HasItemsToEquip = true;
                player.ShouldWaitForShortDelay = true;
            }

            // Handle spells
            if (player.LevelSpellsDict.TryGetValue(playerLevel, out List<int> spellIds))
                player.LuaCall(string.Join(" ", spellIds.Select(id => $"SendChatMessage('.player learn {player.Name} {id}');")));

            // Handle talents
            // https://wowwiki-archive.fandom.com/wiki/API_LearnTalent
            if (player.LevelTalentsDict.TryGetValue(playerLevel, out string talentIndex))
                player.LuaCall($"LearnTalent({talentIndex});");

            if (isSecondTry)
                return;

            // Handle teleport
            if (playerLevel == 60)
            {
                player.LuaCall($"SendChatMessage('.npcb wp go 2583')"); // Teleport to Outland
                player.HasEnteredNewMap = true;
            }
            else if (playerLevel == 70)
            {
                player.LuaCall($"SendChatMessage('.npcb wp go 2706')"); // Teleport to Northrend
                player.HasEnteredNewMap = true;
            }
        }

        private void HandleBotStuck(IDependencyContainer container, LocalPlayer player, bool StuckInState)
        {
            // Force teleport to current WP, or to corpse if dead
            if (player.Health <= 1 || player.InGhostForm)
            {
                // Force teleport to corpse
                Console.WriteLine($"Forcing teleport to corpse " + (StuckInState ? "(UseStuckInStateKillswitch)" : "(UseStuckInPositionKillswitch)"));
                player.LuaCall($"SendChatMessage('.go xyz {player.CorpsePosition.X.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Y.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Z.ToString().Replace(',', '.')}', 'GUILD', nil)");
            }
            else
            {
                ForceTeleport(container, player, (StuckInState ? "(UseStuckInStateKillswitch)" : "(UseStuckInPositionKillswitch)"));
                botStates.Pop();
                botStates.Push(new GrindState(botStates, container));
            }
            player.WpStuckCount = 0;
            player.LuaCall("StaticPopup1Button1:Click();"); // Required if stuck at release point in BG

            player.StuckInStateOrPosCount += 1;
            if (player.StuckInStateOrPosCount > 50)
            {
                Stop();
                Console.WriteLine("Bot stuck in state or pos for more than 50 times. Exiting bot...");
                System.Environment.Exit(1); // Console app
                //System.Windows.Forms.Application.Exit(); // // WinForms app
            }
        }

        private void ForceTeleport(IDependencyContainer container, LocalPlayer player, string reason)
        {
            if (player.CurrWpId == 0)
                player.CurrWpId = container.GetCurrentHotspot().Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault().ID;
            if (random.Next(0, 2) == 0)
                player.CurrWpId = container.GetCurrentHotspot().Waypoints.OrderBy(w => player.Position.DistanceTo(w)).ElementAtOrDefault(1).ID;
            Console.WriteLine($"Forcing teleport to WP: {player.CurrWpId} ({reason})!");
            player.LuaCall($"SendChatMessage('.npcb wp go {player.CurrWpId}')");
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
                return result[0] == "0" || result[0] == "1" || result[0] == "2";
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

        private void LogToFile(string fileName, string text)
        {
            var dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(MainViewModel)).CodeBase);
            var path = new UriBuilder(dir).Path;
            var file = Path.Combine(path, fileName);
            string altFileName = "C:\\local\\VisitedWanderNodes.txt";
            if (File.Exists(file))
            {
                using (var sw = File.AppendText(file))
                {
                    sw.WriteLine(text);
                }
            }
            else if (File.Exists(altFileName))
            {
                using (var sw = File.AppendText(altFileName))
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
