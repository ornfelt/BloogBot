using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
/// This namespace contains the UI components for the BloogBot application.
/// </summary>
namespace BloogBot.UI
{
    /// <summary>
    /// Represents the main view model for the application. Implements the INotifyPropertyChanged interface.
    /// </summary>
    /// <summary>
    /// Represents the main view model for the application. Implements the INotifyPropertyChanged interface.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Constant string that represents an error message when a command encounters an error. The error details can be found in the Console.
        /// </summary>
        const string COMMAND_ERROR = "An error occured. See Console for details.";

        /// <summary>
        /// Array of city names including Orgrimmar, Thunder Bluff, Undercity, Stormwind, Darnassus, and Ironforge.
        /// </summary>
        static readonly string[] CityNames = { "Orgrimmar", "Thunder Bluff", "Undercity", "Stormwind", "Darnassus", "Ironforge" };

        /// <summary>
        /// Represents a readonly instance of the BotLoader class.
        /// </summary>
        readonly BotLoader botLoader = new BotLoader();
        /// <summary>
        /// Represents a readonly Probe object.
        /// </summary>
        readonly Probe probe;
        /// <summary>
        /// Represents the settings for the bot, which are read-only.
        /// </summary>
        readonly BotSettings botSettings;
        /// <summary>
        /// Represents a boolean value indicating whether the system is ready for commands.
        /// </summary>
        bool readyForCommands;

        /// <summary>
        /// Initializes the MainViewModel by loading the bot settings, initializing the logger, repository, and Discord client wrapper, and setting up the travel path generator. It also creates a new Probe instance with a callback and killswitch function, and initializes the travel paths, hotspots, NPCs, and bots.
        /// </summary>
        public MainViewModel()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var botSettingsFilePath = Path.Combine(currentFolder, "botSettings.json");
            botSettings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(botSettingsFilePath));
            UpdatePropertiesWithAttribute(typeof(BotSettingAttribute));

            Logger.Initialize(botSettings);
            Repository.Initialize(botSettings.DatabaseType, botSettings.DatabasePath);
            DiscordClientWrapper.Initialize(botSettings);
            TravelPathGenerator.Initialize(() =>
            {
                OnPropertyChanged(nameof(SaveTravelPathCommandEnabled));
            });

            void callback()
            {
                UpdatePropertiesWithAttribute(typeof(ProbeFieldAttribute));
            }
            void killswitch()
            {
                Stop();
            }
            probe = new Probe(callback, killswitch)
            {
                BlacklistedMobIds = Repository.ListBlacklistedMobIds()
            };

            InitializeTravelPaths();
            InitializeHotspots();
            InitializeNpcs();
            ReloadBots();
        }

        /// <summary>
        /// Gets or sets the collection of strings representing the console output.
        /// </summary>
        public ObservableCollection<string> ConsoleOutput { get; } = new ObservableCollection<string>();
        /// <summary>
        /// Gets or sets the collection of bots.
        /// </summary>
        public ObservableCollection<IBot> Bots { get; private set; }
        /// <summary>
        /// Gets or sets the collection of travel paths.
        /// </summary>
        public ObservableCollection<TravelPath> TravelPaths { get; private set; }
        /// <summary>
        /// Gets or sets the collection of hotspots.
        /// </summary>
        public ObservableCollection<Hotspot> Hotspots { get; private set; }
        /// <summary>
        /// Gets or sets the collection of Npcs.
        /// </summary>
        public ObservableCollection<Npc> Npcs { get; private set; }

        /// <summary>
        /// Represents the start command.
        /// </summary>
        #region Commands

        // Start command
        ICommand startCommand;

        /// <summary>
        /// Starts the UI and logs a message indicating that the bot has started.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// UiStart -> Start : call Start
        /// UiStart -> Log : "Bot started!"
        /// \enduml
        /// </remarks>
        void UiStart()
        {
            Start();
            Log("Bot started!");
        }

        /// <summary>
        /// Starts the execution of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Start -> ObjectManager: KillswitchTriggered = false
        /// Start -> CurrentBot: GetDependencyContainer(botSettings, probe, Hotspots)
        /// activate CurrentBot
        /// CurrentBot --> Start: return container
        /// deactivate CurrentBot
        /// Start -> CurrentBot: Start(container, stopCallback)
        /// activate CurrentBot
        /// Start -> Start: OnPropertyChanged(nameof(StartCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(StopCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(StartPowerlevelCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(StartTravelPathCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(StopTravelPathCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(ReloadBotsCommandEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(CurrentBotEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(GrindingHotspotEnabled))
        /// Start -> Start: OnPropertyChanged(nameof(CurrentTravelPathEnabled))
        /// deactivate CurrentBot
        /// alt Exception
        ///   Start -> Logger: Log(e)
        ///   Start -> Start: Log(COMMAND_ERROR)
        /// end alt
        /// \enduml
        /// </remarks>
        void Start()
        {
            try
            {
                ObjectManager.KillswitchTriggered = false;

                var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

                void stopCallback()
                {
                    OnPropertyChanged(nameof(StartCommandEnabled));
                    OnPropertyChanged(nameof(StopCommandEnabled));
                    OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                    OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                    OnPropertyChanged(nameof(CurrentBotEnabled));
                    OnPropertyChanged(nameof(GrindingHotspotEnabled));
                    OnPropertyChanged(nameof(CurrentTravelPathEnabled));
                }

                currentBot.Start(container, stopCallback);

                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                OnPropertyChanged(nameof(CurrentBotEnabled));
                OnPropertyChanged(nameof(GrindingHotspotEnabled));
                OnPropertyChanged(nameof(CurrentTravelPathEnabled));
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the start command.
        /// </summary>
        public ICommand StartCommand =>
                    startCommand ?? (startCommand = new CommandHandler(UiStart, true));

        /// <summary>
        /// Represents a stop command.
        /// </summary>
        // Stop command
        ICommand stopCommand;

        /// <summary>
        /// Stops the UI and logs a message indicating that the bot has stopped.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// UiStop -> Stop : Call Stop
        /// UiStop -> Log : "Bot stopped!"
        /// \enduml
        /// </remarks>
        void UiStop()
        {
            Stop();
            Log("Bot stopped!");
        }

        /// <summary>
        /// Stops the current bot and updates the enabled properties for various commands and settings.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Stop -> CurrentBot: GetDependencyContainer(botSettings, probe, Hotspots)
        /// Stop -> currentBot: Stop()
        /// Stop -> OnPropertyChanged: StartCommandEnabled
        /// Stop -> OnPropertyChanged: StopCommandEnabled
        /// Stop -> OnPropertyChanged: StartPowerlevelCommandEnabled
        /// Stop -> OnPropertyChanged: StartTravelPathCommandEnabled
        /// Stop -> OnPropertyChanged: StopTravelPathCommandEnabled
        /// Stop -> OnPropertyChanged: ReloadBotsCommandEnabled
        /// Stop -> OnPropertyChanged: CurrentBotEnabled
        /// Stop -> OnPropertyChanged: GrindingHotspotEnabled
        /// Stop -> OnPropertyChanged: CurrentTravelPathEnabled
        /// Stop -> Logger: Log(e)
        /// \enduml
        /// </remarks>
        void Stop()
        {
            try
            {
                var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

                currentBot.Stop();

                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                OnPropertyChanged(nameof(CurrentBotEnabled));
                OnPropertyChanged(nameof(GrindingHotspotEnabled));
                OnPropertyChanged(nameof(CurrentTravelPathEnabled));
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        /// <summary>
        /// Gets the stop command.
        /// </summary>
        public ICommand StopCommand =>
                    stopCommand ?? (stopCommand = new CommandHandler(UiStop, true));

        /// <summary>
        /// Represents a command to reload the bot.
        /// </summary>
        // ReloadBot command
        ICommand reloadBotsCommand;

        /// <summary>
        /// Reloads the bots by creating a new ObservableCollection of IBot objects using the botLoader.ReloadBots() method. 
        /// Sets the CurrentBot property to the first bot in the collection that has a matching Name with the botSettings.CurrentBotName, 
        /// or the first bot in the collection if no match is found. 
        /// Raises property change notifications for the Bots, StartCommandEnabled, StopCommandEnabled, StartPowerlevelCommandEnabled, 
        /// and ReloadBotsCommandEnabled properties. 
        /// Logs "Bot successfully loaded!" if the bots are successfully reloaded. 
        /// Catches any exceptions that occur, logs them using the Logger.Log() method, and logs the COMMAND_ERROR message.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ReloadBots()" as A
        /// participant "botLoader" as B
        /// participant "Bots" as C
        /// participant "CurrentBot" as D
        /// participant "Logger" as E
        /// 
        /// A -> B: ReloadBots()
        /// activate B
        /// B --> A: Return Bots
        /// deactivate B
        /// 
        /// A -> C: Create ObservableCollection
        /// activate C
        /// C --> A: Return ObservableCollection
        /// deactivate C
        /// 
        /// A -> D: Assign CurrentBot
        /// activate D
        /// D --> A: Return CurrentBot
        /// deactivate D
        /// 
        /// A -> A: OnPropertyChanged("Bots")
        /// A -> A: OnPropertyChanged("StartCommandEnabled")
        /// A -> A: OnPropertyChanged("StopCommandEnabled")
        /// A -> A: OnPropertyChanged("StartPowerlevelCommandEnabled")
        /// A -> A: OnPropertyChanged("ReloadBotsCommandEnabled")
        /// 
        /// A -> A: Log("Bot successfully loaded!")
        /// 
        /// alt Exception Occurs
        ///   A -> E: Log(e)
        ///   A -> A: Log(COMMAND_ERROR)
        /// end
        /// \enduml
        /// </remarks>
        void ReloadBots()
        {
            try
            {
                Bots = new ObservableCollection<IBot>(botLoader.ReloadBots());
                CurrentBot = Bots.FirstOrDefault(b => b.Name == botSettings.CurrentBotName) ?? Bots.First();

                OnPropertyChanged(nameof(Bots));
                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));

                Log("Bot successfully loaded!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to reload bots. If the command is null, it creates a new instance of CommandHandler and assigns it to the command.
        /// </summary>
        public ICommand ReloadBotsCommand =>
                    reloadBotsCommand ?? (reloadBotsCommand = new CommandHandler(ReloadBots, true));

        /// <summary>
        /// Represents a command to blacklist the current target.
        /// </summary>
        ICommand blacklistCurrentTargetCommand;

        /// <summary>
        /// Blacklists the current target. If the target is already blacklisted, it will be removed from the blacklist. If the target is not blacklisted, it will be added to the blacklist.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// BlacklistCurrentTarget -> ObjectManager: Get CurrentTarget
        /// ObjectManager --> BlacklistCurrentTarget: target
        /// if target is not null then
        ///     BlacklistCurrentTarget -> Repository: Check if BlacklistedMobExists(target.Guid)
        ///     Repository --> BlacklistCurrentTarget: exists
        ///     if exists then
        ///         BlacklistCurrentTarget -> Log: Log "Target already blacklisted. Removing from blacklist."
        ///         BlacklistCurrentTarget -> Repository: RemoveBlacklistedMob(target.Guid)
        ///         Repository --> BlacklistCurrentTarget: removed
        ///         BlacklistCurrentTarget -> probe: Remove target.Guid from BlacklistedMobIds
        ///     else
        ///         BlacklistCurrentTarget -> Repository: AddBlacklistedMob(target.Guid)
        ///         Repository --> BlacklistCurrentTarget: added
        ///         BlacklistCurrentTarget -> probe: Add target.Guid to BlacklistedMobIds
        ///         BlacklistCurrentTarget -> Log: Log "Successfully blacklisted mob: target.Guid"
        ///     endif
        /// else
        ///     BlacklistCurrentTarget -> Log: Log "Blacklist failed. No target selected."
        /// endif
        /// BlacklistCurrentTarget -> Logger: Log exception e
        /// BlacklistCurrentTarget -> Log: Log COMMAND_ERROR
        /// \enduml
        /// </remarks>
        void BlacklistCurrentTarget()
        {
            try
            {
                var target = ObjectManager.CurrentTarget;
                if (target != null)
                {
                    if (Repository.BlacklistedMobExists(target.Guid))
                    {
                        Log("Target already blacklisted. Removing from blacklist.");
                        Repository.RemoveBlacklistedMob(target.Guid);
                        probe.BlacklistedMobIds.Remove(target.Guid);
                    }
                    else
                    {
                        Repository.AddBlacklistedMob(target.Guid);
                        probe.BlacklistedMobIds.Add(target.Guid);
                        Log($"Successfully blacklisted mob: {target.Guid}");
                    }
                }
                else
                    Log("Blacklist failed. No target selected.");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to blacklist the current target. If the command is null, it creates a new instance of the CommandHandler class with the BlacklistCurrentTarget method and sets it as the value of the command.
        /// </summary>
        public ICommand BlacklistCurrentTargetCommand =>
                    blacklistCurrentTargetCommand ?? (blacklistCurrentTargetCommand = new CommandHandler(BlacklistCurrentTarget, true));

        /// <summary>
        /// Represents a test command.
        /// </summary>
        // Test command
        ICommand testCommand;

        /// <summary>
        /// This method is used to test the functionality of the current bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CurrentBot -> Test: GetDependencyContainer(botSettings, probe, Hotspots)
        /// Test -> currentBot: Test(container)
        /// \enduml
        /// </remarks>
        void Test()
        {
            var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

            currentBot.Test(container);
        }

        /// <summary>
        /// Gets the test command.
        /// </summary>
        public ICommand TestCommand =>
                    testCommand ?? (testCommand = new CommandHandler(Test, true));

        /// <summary>
        /// Represents a command to start the power level.
        /// </summary>
        // StartPowerlevel command
        ICommand startPowerlevelCommand;

        /// <summary>
        /// Starts the powerlevel process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// StartPowerlevel -> CurrentBot: GetDependencyContainer(botSettings, probe, Hotspots)
        /// StartPowerlevel -> CurrentBot: StartPowerlevel(container, stopCallback)
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(StartCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(StopCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(StartPowerlevelCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(StartTravelPathCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(StopTravelPathCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(ReloadBotsCommandEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(CurrentBotEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(GrindingHotspotEnabled))
        /// StartPowerlevel -> StartPowerlevel: OnPropertyChanged(nameof(CurrentTravelPathEnabled))
        /// \enduml
        /// </remarks>
        void StartPowerlevel()
        {
            var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

            void stopCallback()
            {
                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                OnPropertyChanged(nameof(CurrentBotEnabled));
                OnPropertyChanged(nameof(GrindingHotspotEnabled));
                OnPropertyChanged(nameof(CurrentTravelPathEnabled));
            }

            currentBot.StartPowerlevel(container, stopCallback);

            OnPropertyChanged(nameof(StartCommandEnabled));
            OnPropertyChanged(nameof(StopCommandEnabled));
            OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
            OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
            OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
            OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
            OnPropertyChanged(nameof(CurrentBotEnabled));
            OnPropertyChanged(nameof(GrindingHotspotEnabled));
            OnPropertyChanged(nameof(CurrentTravelPathEnabled));
        }

        /// <summary>
        /// Gets the command to start the power level.
        /// </summary>
        public ICommand StartPowerlevelCommand =>
                    startPowerlevelCommand ?? (startPowerlevelCommand = new CommandHandler(StartPowerlevel, true));

        /// <summary>
        /// Saves the settings.
        /// </summary>
        // SaveSettings command
        ICommand saveSettingsCommand;

        /// <summary>
        /// Saves the current settings to a JSON file.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// SaveSettings -> botSettings: Set CurrentTravelPathId, GrindingHotspotId, CurrentBotName
        /// SaveSettings -> Assembly: GetExecutingAssembly().Location
        /// SaveSettings -> Path: Combine(currentFolder, "botSettings.json")
        /// SaveSettings -> JsonConvert: SerializeObject(botSettings, Formatting.Indented)
        /// SaveSettings -> File: WriteAllText(botSettingsFilePath, json)
        /// SaveSettings -> Log: "Settings successfully saved!"
        /// SaveSettings -> Logger: Log(e)
        /// SaveSettings -> Log: COMMAND_ERROR
        /// \enduml
        /// </remarks>
        void SaveSettings()
        {
            try
            {
                botSettings.CurrentTravelPathId = CurrentTravelPath?.Id;
                botSettings.GrindingHotspotId = GrindingHotspot?.Id;
                botSettings.CurrentBotName = CurrentBot.Name;

                var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var botSettingsFilePath = Path.Combine(currentFolder, "botSettings.json");
                var json = JsonConvert.SerializeObject(botSettings, Formatting.Indented);
                File.WriteAllText(botSettingsFilePath, json);

                Log("Settings successfully saved!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to save the settings.
        /// </summary>
        public ICommand SaveSettingsCommand =>
                    saveSettingsCommand ?? (saveSettingsCommand = new CommandHandler(SaveSettings, true));

        /// <summary>
        /// Represents a command to start recording a travel path.
        /// </summary>
        // StartRecordingTravelPath command
        ICommand startRecordingTravelPathCommand;

        /// <summary>
        /// Starts recording the travel path.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "StartRecordingTravelPath()" as SRT
        /// participant "ThreadSynchronizer" as TS
        /// participant "Functions" as F
        /// participant "TravelPathGenerator" as TPG
        /// participant "ObjectManager" as OM
        /// participant "Logger" as L
        ///
        /// SRT -> TS : RunOnMainThread()
        /// TS -> F : GetPlayerGuid()
        /// F -> TS : return Guid
        /// TS -> SRT : return isLoggedIn
        /// 
        /// alt isLoggedIn is true
        ///     SRT -> TPG : Record(Player, Log)
        ///     SRT -> SRT : OnPropertyChanged(StartRecordingTravelPathCommandEnabled)
        ///     SRT -> SRT : OnPropertyChanged(SaveTravelPathCommandEnabled)
        ///     SRT -> SRT : OnPropertyChanged(CancelTravelPathCommandEnabled)
        ///     SRT -> SRT : Log("Recording new travel path...")
        /// else isLoggedIn is false
        ///     SRT -> SRT : Log("Recording failed. Not logged in.")
        /// end
        /// 
        /// alt Exception occurs
        ///     SRT -> L : Log(e)
        ///     SRT -> SRT : Log(COMMAND_ERROR)
        /// end
        /// \enduml
        /// </remarks>
        void StartRecordingTravelPath()
        {
            try
            {
                var isLoggedIn = ThreadSynchronizer.RunOnMainThread(() => Functions.GetPlayerGuid() > 0);
                if (isLoggedIn)
                {
                    TravelPathGenerator.Record(ObjectManager.Player, Log);

                    OnPropertyChanged(nameof(StartRecordingTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(SaveTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(CancelTravelPathCommandEnabled));

                    Log("Recording new travel path...");

                }
                else
                    Log("Recording failed. Not logged in.");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to start recording the travel path. If the command is null, it creates a new instance of the CommandHandler class and assigns it to the command.
        /// </summary>
        public ICommand StartRecordingTravelPathCommand =>
                    startRecordingTravelPathCommand ?? (startRecordingTravelPathCommand = new CommandHandler(StartRecordingTravelPath, true));

        /// <summary>
        /// Represents a command to cancel a travel path.
        /// </summary>
        // CancelTravelPath command
        ICommand cancelTravelPathCommand;

        /// <summary>
        /// Cancels the current travel path generation.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CancelTravelPath -> TravelPathGenerator: Cancel()
        /// CancelTravelPath -> CancelTravelPath: OnPropertyChanged(nameof(StartRecordingTravelPathCommandEnabled))
        /// CancelTravelPath -> CancelTravelPath: OnPropertyChanged(nameof(SaveTravelPathCommandEnabled))
        /// CancelTravelPath -> CancelTravelPath: OnPropertyChanged(nameof(CancelTravelPathCommandEnabled))
        /// CancelTravelPath -> CancelTravelPath: Log("Canceling new travel path...")
        /// CancelTravelPath -> Logger: Log(e)
        /// CancelTravelPath -> CancelTravelPath: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void CancelTravelPath()
        {
            try
            {
                TravelPathGenerator.Cancel();

                OnPropertyChanged(nameof(StartRecordingTravelPathCommandEnabled));
                OnPropertyChanged(nameof(SaveTravelPathCommandEnabled));
                OnPropertyChanged(nameof(CancelTravelPathCommandEnabled));

                Log("Canceling new travel path...");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the cancel travel path command.
        /// </summary>
        public ICommand CancelTravelPathCommand =>
                    cancelTravelPathCommand ?? (cancelTravelPathCommand = new CommandHandler(CancelTravelPath, true));

        /// <summary>
        /// Saves the travel path.
        /// </summary>
        // SaveTravelPath command
        ICommand saveTravelPathCommand;

        /// <summary>
        /// Saves the current travel path by calling the TravelPathGenerator.Save() method and adding it to the repository. 
        /// Updates the list of travel paths and clears the new travel path name. 
        /// Notifies property changes for StartRecordingTravelPathCommandEnabled, SaveTravelPathCommandEnabled, CancelTravelPathCommandEnabled, and TravelPaths. 
        /// Logs a message indicating the successful saving of the new travel path. 
        /// If an exception occurs, logs the exception and logs an error message.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// SaveTravelPath -> TravelPathGenerator: Save()
        /// TravelPathGenerator --> SaveTravelPath: waypoints
        /// SaveTravelPath -> Repository: AddTravelPath(newTravelPathName, waypoints)
        /// Repository --> SaveTravelPath: travelPath
        /// SaveTravelPath -> TravelPaths: Add(travelPath)
        /// SaveTravelPath -> TravelPaths: OrderBy(p => p?.Name)
        /// SaveTravelPath -> SaveTravelPath: OnPropertyChanged(nameof(StartRecordingTravelPathCommandEnabled))
        /// SaveTravelPath -> SaveTravelPath: OnPropertyChanged(nameof(SaveTravelPathCommandEnabled))
        /// SaveTravelPath -> SaveTravelPath: OnPropertyChanged(nameof(CancelTravelPathCommandEnabled))
        /// SaveTravelPath -> SaveTravelPath: OnPropertyChanged(nameof(TravelPaths))
        /// SaveTravelPath -> SaveTravelPath: Log("New travel path successfully saved!")
        /// SaveTravelPath -> Logger: Log(e)
        /// SaveTravelPath -> SaveTravelPath: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void SaveTravelPath()
        {
            try
            {
                var waypoints = TravelPathGenerator.Save();
                var travelPath = Repository.AddTravelPath(newTravelPathName, waypoints);

                TravelPaths.Add(travelPath);
                TravelPaths = new ObservableCollection<TravelPath>(TravelPaths.OrderBy(p => p?.Name));

                NewTravelPathName = string.Empty;

                OnPropertyChanged(nameof(StartRecordingTravelPathCommandEnabled));
                OnPropertyChanged(nameof(SaveTravelPathCommandEnabled));
                OnPropertyChanged(nameof(CancelTravelPathCommandEnabled));
                OnPropertyChanged(nameof(TravelPaths));

                Log("New travel path successfully saved!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to save the travel path.
        /// </summary>
        public ICommand SaveTravelPathCommand =>
                    saveTravelPathCommand ?? (saveTravelPathCommand = new CommandHandler(SaveTravelPath, true));

        /// <summary>
        /// Executes the StartTravelPath command.
        /// </summary>
        // StartTravelPath
        ICommand startTravelPathCommand;

        /// <summary>
        /// Starts the travel path.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// StartTravelPath -> CurrentBot: GetDependencyContainer(botSettings, probe, Hotspots)
        /// StartTravelPath -> CurrentBot: Travel(container, reverseTravelPath, callback)
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(StartCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(StopCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(StartPowerlevelCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(StartTravelPathCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(StopTravelPathCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(ReloadBotsCommandEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(CurrentBotEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(GrindingHotspotEnabled))
        /// StartTravelPath -> StartTravelPath: OnPropertyChanged(nameof(CurrentTravelPathEnabled))
        /// StartTravelPath -> Logger: Log(e)
        /// StartTravelPath -> StartTravelPath: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void StartTravelPath()
        {
            try
            {
                var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

                void callback()
                {
                    OnPropertyChanged(nameof(StartCommandEnabled));
                    OnPropertyChanged(nameof(StopCommandEnabled));
                    OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                    OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                    OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                    OnPropertyChanged(nameof(CurrentBotEnabled));
                    OnPropertyChanged(nameof(GrindingHotspotEnabled));
                    OnPropertyChanged(nameof(CurrentTravelPathEnabled));
                }

                currentBot.Travel(container, reverseTravelPath, callback);

                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                OnPropertyChanged(nameof(CurrentBotEnabled));
                OnPropertyChanged(nameof(GrindingHotspotEnabled));
                OnPropertyChanged(nameof(CurrentTravelPathEnabled));

                Log("Travel started!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to start the travel path.
        /// </summary>
        public ICommand StartTravelPathCommand =>
                    startTravelPathCommand ?? (startTravelPathCommand = new CommandHandler(StartTravelPath, true));

        /// <summary>
        /// Stops the travel path.
        /// </summary>
        // StopTravelPath
        ICommand stopTravelPathCommand;

        /// <summary>
        /// Stops the current travel path.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// StopTravelPath -> CurrentBot: GetDependencyContainer(botSettings, probe, Hotspots)
        /// StopTravelPath -> currentBot: Stop()
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(StartCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(StopCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(StartPowerlevelCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(StartTravelPathCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(StopTravelPathCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(ReloadBotsCommandEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(CurrentBotEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(GrindingHotspotEnabled))
        /// StopTravelPath -> StopTravelPath: OnPropertyChanged(nameof(CurrentTravelPathEnabled))
        /// StopTravelPath -> StopTravelPath: Log("TravelPath stopped!")
        /// StopTravelPath -> Logger: Log(e)
        /// StopTravelPath -> StopTravelPath: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void StopTravelPath()
        {
            try
            {
                var container = CurrentBot.GetDependencyContainer(botSettings, probe, Hotspots);

                currentBot.Stop();

                OnPropertyChanged(nameof(StartCommandEnabled));
                OnPropertyChanged(nameof(StopCommandEnabled));
                OnPropertyChanged(nameof(StartPowerlevelCommandEnabled));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
                OnPropertyChanged(nameof(StopTravelPathCommandEnabled));
                OnPropertyChanged(nameof(ReloadBotsCommandEnabled));
                OnPropertyChanged(nameof(CurrentBotEnabled));
                OnPropertyChanged(nameof(GrindingHotspotEnabled));
                OnPropertyChanged(nameof(CurrentTravelPathEnabled));

                Log("TravelPath stopped!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to stop the travel path.
        /// </summary>
        public ICommand StopTravelPathCommand =>
                    stopTravelPathCommand ?? (stopTravelPathCommand = new CommandHandler(StopTravelPath, true));

        /// <summary>
        /// Clears the log.
        /// </summary>
        // ClearLog
        ICommand clearLogCommand;

        /// <summary>
        /// Clears the console output and updates the property for console output.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ClearLog -> ConsoleOutput: Clear()
        /// ClearLog -> ClearLog: OnPropertyChanged(nameof(ConsoleOutput))
        /// alt Exception
        ///   ClearLog -> Logger: Log(e)
        ///   ClearLog -> ClearLog: Log(COMMAND_ERROR)
        /// end alt
        /// \enduml
        /// </remarks>
        void ClearLog()
        {
            try
            {
                ConsoleOutput.Clear();
                OnPropertyChanged(nameof(ConsoleOutput));
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to clear the log.
        /// </summary>
        public ICommand ClearLogCommand =>
                    clearLogCommand ?? (clearLogCommand = new CommandHandler(ClearLog, true));

        /// <summary>
        /// Adds an NPC to the game.
        /// </summary>
        // AddNpc
        ICommand addNpcCommand;

        /// <summary>
        /// Adds an NPC to the repository if the target is not null and the NPC does not already exist.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddNpc -> ObjectManager: CurrentTarget
        /// ObjectManager --> AddNpc: target
        /// AddNpc -> Repository: NpcExists(target.Name)
        /// Repository --> AddNpc: NpcExistsResult
        /// alt NpcExistsResult is true
        ///     AddNpc -> AddNpc: Log("NPC already exists!")
        /// else NpcExistsResult is false
        ///     AddNpc -> Repository: AddNpc(target.Name, npcIsInnkeeper, npcSellsAmmo, npcRepairs, false, npcHorde, npcAlliance, target.Position.X, target.Position.Y, target.Position.Z, ObjectManager.ZoneText)
        ///     Repository --> AddNpc: npc
        ///     AddNpc -> Npcs: Add(npc)
        ///     AddNpc -> Npcs: OrderBy(n => n?.Horde).ThenBy(n => n?.Name)
        ///     AddNpc -> AddNpc: OnPropertyChanged(nameof(Npcs))
        ///     AddNpc -> AddNpc: Log("NPC saved successfully!")
        /// else target is null
        ///     AddNpc -> AddNpc: Log("NPC not saved. No target selected.")
        /// end
        /// AddNpc -> Logger: Log(e)
        /// AddNpc -> AddNpc: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void AddNpc()
        {
            try
            {
                var target = ObjectManager.CurrentTarget;
                if (target != null)
                {
                    if (Repository.NpcExists(target.Name))
                    {
                        Log("NPC already exists!");
                        return;
                    }

                    var npc = Repository.AddNpc(
                        target.Name,
                        npcIsInnkeeper,
                        npcSellsAmmo,
                        npcRepairs,
                        false, // npcQuest - deprecated
                        npcHorde,
                        npcAlliance,
                        target.Position.X,
                        target.Position.Y,
                        target.Position.Z,
                        ObjectManager.ZoneText);

                    Npcs.Add(npc);
                    Npcs = new ObservableCollection<Npc>(Npcs.OrderBy(n => n?.Horde)
                        .ThenBy(n => n?.Name));

                    OnPropertyChanged(nameof(Npcs));

                    Log("NPC saved successfully!");
                }
                else
                    Log("NPC not saved. No target selected.");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command for adding an NPC.
        /// </summary>
        public ICommand AddNpcCommand =>
                    addNpcCommand ?? (addNpcCommand = new CommandHandler(AddNpc, true));

        /// <summary>
        /// Represents a command to start recording a hotspot.
        /// </summary>
        // RecordHotspot
        ICommand startRecordingHotspotCommand;

        /// <summary>
        /// Starts recording a new hotspot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// StartRecordingHotspot -> ThreadSynchronizer: RunOnMainThread
        /// ThreadSynchronizer --> StartRecordingHotspot: isLoggedIn
        /// alt isLoggedIn is true
        ///     StartRecordingHotspot -> HotspotGenerator: Record
        ///     StartRecordingHotspot -> StartRecordingHotspot: OnPropertyChanged(StartRecordingHotspotCommandEnabled)
        ///     StartRecordingHotspot -> StartRecordingHotspot: OnPropertyChanged(AddHotspotWaypointCommandEnabled)
        ///     StartRecordingHotspot -> StartRecordingHotspot: OnPropertyChanged(SaveHotspotCommandEnabled)
        ///     StartRecordingHotspot -> StartRecordingHotspot: OnPropertyChanged(CancelHotspotCommandEnabled)
        ///     StartRecordingHotspot -> StartRecordingHotspot: Log("Recording new hotspot...")
        /// else isLoggedIn is false
        ///     StartRecordingHotspot -> StartRecordingHotspot: Log("Recording failed. Not logged in.")
        /// end
        /// alt Exception occurs
        ///     StartRecordingHotspot -> Logger: Log(e)
        ///     StartRecordingHotspot -> StartRecordingHotspot: Log(COMMAND_ERROR)
        /// end
        /// \enduml
        /// </remarks>
        void StartRecordingHotspot()
        {
            try
            {
                var isLoggedIn = ThreadSynchronizer.RunOnMainThread(() => Functions.GetPlayerGuid() > 0);
                if (isLoggedIn)
                {
                    HotspotGenerator.Record();

                    OnPropertyChanged(nameof(StartRecordingHotspotCommandEnabled));
                    OnPropertyChanged(nameof(AddHotspotWaypointCommandEnabled));
                    OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
                    OnPropertyChanged(nameof(CancelHotspotCommandEnabled));

                    Log("Recording new hotspot...");

                }
                else
                    Log("Recording failed. Not logged in.");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to start recording a hotspot.
        /// </summary>
        public ICommand StartRecordingHotspotCommand =>
                    startRecordingHotspotCommand ?? (startRecordingHotspotCommand = new CommandHandler(StartRecordingHotspot, true));

        /// <summary>
        /// Adds a hotspot waypoint.
        /// </summary>
        // AddHotspotWaypoint
        ICommand addHotspotWaypointCommand;

        /// <summary>
        /// Adds a hotspot waypoint to the player's current position.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddHotspotWaypoint -> ThreadSynchronizer: RunOnMainThread
        /// ThreadSynchronizer --> AddHotspotWaypoint: isLoggedIn
        /// alt isLoggedIn is true
        ///     AddHotspotWaypoint -> HotspotGenerator: AddWaypoint
        ///     AddHotspotWaypoint -> AddHotspotWaypoint: OnPropertyChanged
        ///     AddHotspotWaypoint -> AddHotspotWaypoint: Log("Waypoint successfully added!")
        /// else isLoggedIn is false
        ///     AddHotspotWaypoint -> AddHotspotWaypoint: Log("Failed to add waypoint. Not logged in.")
        /// end
        /// AddHotspotWaypoint -> Logger: Log(e)
        /// AddHotspotWaypoint -> AddHotspotWaypoint: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void AddHotspotWaypoint()
        {
            try
            {
                var isLoggedIn = ThreadSynchronizer.RunOnMainThread(() => Functions.GetPlayerGuid() > 0);
                if (isLoggedIn)
                {
                    HotspotGenerator.AddWaypoint(ObjectManager.Player.Position);

                    OnPropertyChanged(nameof(SaveHotspotCommandEnabled));

                    Log("Waypoint successfully added!");

                }
                else
                    Log("Failed to add waypoint. Not logged in.");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command for adding a hotspot waypoint.
        /// </summary>
        public ICommand AddHotspotWaypointCommand =>
                    addHotspotWaypointCommand ?? (addHotspotWaypointCommand = new CommandHandler(AddHotspotWaypoint, true));

        /// <summary>
        /// Saves the hotspot.
        /// </summary>
        // SaveHotspot
        ICommand saveHotspotCommand;

        /// <summary>
        /// Saves the current hotspot with the specified description, faction, waypoints, innkeeper, repair vendor, ammo vendor, minimum level, travel path, and safe for grinding flag.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// SaveHotspot -> HotspotGenerator: Save()
        /// SaveHotspot -> Repository: AddHotspot()
        /// SaveHotspot -> Hotspots: Add(hotspot)
        /// SaveHotspot -> Hotspots: OrderBy()
        /// SaveHotspot -> OnPropertyChanged: StartRecordingHotspotCommandEnabled
        /// SaveHotspot -> OnPropertyChanged: AddHotspotWaypointCommandEnabled
        /// SaveHotspot -> OnPropertyChanged: SaveHotspotCommandEnabled
        /// SaveHotspot -> OnPropertyChanged: CancelHotspotCommandEnabled
        /// SaveHotspot -> OnPropertyChanged: Hotspots
        /// SaveHotspot -> Log: "New hotspot successfully saved!"
        /// SaveHotspot -> Logger: Log(e)
        /// SaveHotspot -> Log: COMMAND_ERROR
        /// \enduml
        /// </remarks>
        void SaveHotspot()
        {
            try
            {
                string faction;
                if (newHotspotHorde && newHotspotAlliance)
                    faction = "Alliance / Horde";
                else if (newHotspotHorde)
                    faction = "Horde";
                else
                    faction = "Alliance";

                var waypoints = HotspotGenerator.Save();
                var hotspot = Repository.AddHotspot(
                    ObjectManager.ZoneText,
                    newHotspotDescription,
                    faction,
                    waypoints,
                    newHotspotInnkeeper,
                    newHotspotRepairVendor,
                    newHotspotAmmoVendor,
                    newHotspotMinLevel,
                    newHotspotTravelPath,
                    newHotspotSafeForGrinding);

                Hotspots.Add(hotspot);
                Hotspots = new ObservableCollection<Hotspot>(Hotspots.OrderBy(h => h?.MinLevel).ThenBy(h => h?.Zone).ThenBy(h => h?.Description));

                NewHotspotDescription = string.Empty;
                NewHotspotMinLevel = 0;
                NewHotspotInnkeeper = null;
                NewHotspotRepairVendor = null;
                NewHotspotAmmoVendor = null;
                NewHotspotTravelPath = null;
                NewHotspotHorde = false;
                NewHotspotAlliance = false;

                OnPropertyChanged(nameof(StartRecordingHotspotCommandEnabled));
                OnPropertyChanged(nameof(AddHotspotWaypointCommandEnabled));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
                OnPropertyChanged(nameof(CancelHotspotCommandEnabled));
                OnPropertyChanged(nameof(Hotspots));

                Log("New hotspot successfully saved!");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the command to save a hotspot. If the command is null, it creates a new instance of the CommandHandler class with the SaveHotspot method and sets the command to true.
        /// </summary>
        public ICommand SaveHotspotCommand =>
                    saveHotspotCommand ?? (saveHotspotCommand = new CommandHandler(SaveHotspot, true));

        /// <summary>
        /// Gets or sets the command to cancel the hotspot.
        /// </summary>
        // CancelHotspot
        ICommand cancelHotspotCommand;

        /// <summary>
        /// Cancels the hotspot generation process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CancelHotspot -> HotspotGenerator: Cancel()
        /// CancelHotspot -> CancelHotspot: OnPropertyChanged("StartRecordingHotspotCommandEnabled")
        /// CancelHotspot -> CancelHotspot: OnPropertyChanged("AddHotspotWaypointCommandEnabled")
        /// CancelHotspot -> CancelHotspot: OnPropertyChanged("SaveHotspotCommandEnabled")
        /// CancelHotspot -> CancelHotspot: OnPropertyChanged("CancelHotspotCommandEnabled")
        /// CancelHotspot -> CancelHotspot: Log("Canceling new travel path...")
        /// CancelHotspot -> Logger: Log(e)
        /// CancelHotspot -> CancelHotspot: Log(COMMAND_ERROR)
        /// \enduml
        /// </remarks>
        void CancelHotspot()
        {
            try
            {
                HotspotGenerator.Cancel();

                OnPropertyChanged(nameof(StartRecordingHotspotCommandEnabled));
                OnPropertyChanged(nameof(AddHotspotWaypointCommandEnabled));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
                OnPropertyChanged(nameof(CancelHotspotCommandEnabled));

                Log("Canceling new travel path...");
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Log(COMMAND_ERROR);
            }
        }

        /// <summary>
        /// Gets the cancel hotspot command.
        /// </summary>
        public ICommand CancelHotspotCommand =>
                    cancelHotspotCommand ?? (cancelHotspotCommand = new CommandHandler(CancelHotspot, true));

        /// <summary>
        /// Gets a value indicating whether the AddNpcCommand is enabled.
        /// </summary>
        #endregion

        #region Observables
        // IsEnabled
        public bool AddNpcCommandEnabled =>
            (npcIsInnkeeper || npcSellsAmmo || npcRepairs) &&
            (npcHorde || npcAlliance);

        /// <summary>
        /// Gets a value indicating whether the StartRecordingTravelPath command is enabled.
        /// </summary>
        public bool StartRecordingTravelPathCommandEnabled => !TravelPathGenerator.Recording;

        /// <summary>
        /// Gets a value indicating whether the save travel path command is enabled.
        /// </summary>
        public bool SaveTravelPathCommandEnabled =>
                    TravelPathGenerator.Recording &&
                    TravelPathGenerator.PositionCount > 0 &&
                    !string.IsNullOrWhiteSpace(newTravelPathName);

        /// <summary>
        /// Gets a value indicating whether the cancel travel path command is enabled.
        /// </summary>
        public bool CancelTravelPathCommandEnabled => TravelPathGenerator.Recording;

        /// <summary>
        /// Gets a value indicating whether the StartTravelPathCommand is enabled.
        /// </summary>
        public bool StartTravelPathCommandEnabled =>
                    !currentBot.Running() &&
                    CurrentTravelPath != null;

        /// <summary>
        /// Gets a value indicating whether the stop travel path command is enabled.
        /// </summary>
        public bool StopTravelPathCommandEnabled => currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the current travel path is enabled.
        /// </summary>
        public bool CurrentTravelPathEnabled => !currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the start command is enabled.
        /// </summary>
        public bool StartCommandEnabled => !currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the stop command is enabled.
        /// </summary>
        public bool StopCommandEnabled => currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the StartPowerlevelCommand is enabled.
        /// </summary>
        public bool StartPowerlevelCommandEnabled => !currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the reload bots command is enabled.
        /// </summary>
        public bool ReloadBotsCommandEnabled => !currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the Start Recording Hotspot command is enabled.
        /// </summary>
        public bool StartRecordingHotspotCommandEnabled =>
                    !HotspotGenerator.Recording;

        /// <summary>
        /// Gets a value indicating whether the AddHotspotWaypointCommand is enabled.
        /// </summary>
        public bool AddHotspotWaypointCommandEnabled =>
                    HotspotGenerator.Recording;

        /// <summary>
        /// Determines if the save hotspot command is enabled.
        /// </summary>
        public bool SaveHotspotCommandEnabled =>
                    HotspotGenerator.Recording &&
                    HotspotGenerator.PositionCount > 0 &&
                    !string.IsNullOrWhiteSpace(newHotspotDescription) &&
                    newHotspotMinLevel > 0 &&
                    (newHotspotHorde || newHotspotAlliance);

        /// <summary>
        /// Gets a value indicating whether the cancel hotspot command is enabled.
        /// </summary>
        public bool CancelHotspotCommandEnabled =>
                    HotspotGenerator.Recording;

        /// <summary>
        /// Gets a value indicating whether the current bot is enabled or not.
        /// </summary>
        public bool CurrentBotEnabled => !currentBot.Running();

        /// <summary>
        /// Gets a value indicating whether the grinding hotspot is enabled.
        /// </summary>
        public bool GrindingHotspotEnabled => !currentBot.Running();

        /// <summary>
        /// Represents the current bot.
        /// </summary>
        // General
        IBot currentBot;
        /// <summary>
        /// Gets or sets the current bot.
        /// </summary>
        public IBot CurrentBot
        {
            get => currentBot;
            set
            {
                currentBot = value;
                OnPropertyChanged(nameof(CurrentBot));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether the NPC is an innkeeper.
        /// </summary>
        bool npcIsInnkeeper;
        /// <summary>
        /// Gets or sets a value indicating whether the NPC is an innkeeper.
        /// </summary>
        public bool NpcIsInnkeeper
        {
            get => npcIsInnkeeper;
            set
            {
                npcIsInnkeeper = value;
                OnPropertyChanged(nameof(NpcIsInnkeeper));
                OnPropertyChanged(nameof(AddNpcCommandEnabled));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the NPC sells ammo.
        /// </summary>
        bool npcSellsAmmo;
        /// <summary>
        /// Gets or sets a value indicating whether the NPC sells ammo.
        /// </summary>
        public bool NpcSellsAmmo
        {
            get => npcSellsAmmo;
            set
            {
                npcSellsAmmo = value;
                OnPropertyChanged(nameof(NpcSellsAmmo));
                OnPropertyChanged(nameof(AddNpcCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether the NPC can perform repairs.
        /// </summary>
        bool npcRepairs;
        /// <summary>
        /// Gets or sets a value indicating whether the NPC repairs are enabled.
        /// </summary>
        public bool NpcRepairs
        {
            get => npcRepairs;
            set
            {
                npcRepairs = value;
                OnPropertyChanged(nameof(NpcRepairs));
                OnPropertyChanged(nameof(AddNpcCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether there is an NPC horde.
        /// </summary>
        bool npcHorde;
        /// <summary>
        /// Gets or sets a value indicating whether the NPC horde is enabled.
        /// </summary>
        public bool NpcHorde
        {
            get => npcHorde;
            set
            {
                npcHorde = value;
                OnPropertyChanged(nameof(NpcHorde));
                OnPropertyChanged(nameof(AddNpcCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether the NPC is allied or not.
        /// </summary>
        bool npcAlliance;
        /// <summary>
        /// Gets or sets a value indicating whether the NPC is part of an alliance.
        /// </summary>
        public bool NpcAlliance
        {
            get => npcAlliance;
            set
            {
                npcAlliance = value;
                OnPropertyChanged(nameof(NpcAlliance));
                OnPropertyChanged(nameof(AddNpcCommandEnabled));
            }
        }

        /// <summary>
        /// Represents the name of a new travel path.
        /// </summary>
        string newTravelPathName;
        /// <summary>
        /// Gets or sets the name of the new travel path.
        /// </summary>
        public string NewTravelPathName
        {
            get => newTravelPathName;
            set
            {
                newTravelPathName = value;
                OnPropertyChanged(nameof(NewTravelPathName));
            }
        }

        /// <summary>
        /// Represents a new hotspot description.
        /// </summary>
        string newHotspotDescription;
        /// <summary>
        /// Gets or sets the new hotspot description.
        /// </summary>
        public string NewHotspotDescription
        {
            get => newHotspotDescription;
            set
            {
                newHotspotDescription = value;
                OnPropertyChanged(nameof(NewHotspotDescription));
            }
        }

        /// <summary>
        /// Represents the minimum level required for a new hotspot.
        /// </summary>
        int newHotspotMinLevel;
        /// <summary>
        /// Gets or sets the minimum level for a new hotspot.
        /// </summary>
        public int NewHotspotMinLevel
        {
            get => newHotspotMinLevel;
            set
            {
                newHotspotMinLevel = value;
                OnPropertyChanged(nameof(NewHotspotMinLevel));
            }
        }

        /// <summary>
        /// Represents an NPC character who serves as an innkeeper in a hotspot location.
        /// </summary>
        Npc newHotspotInnkeeper;
        /// <summary>
        /// Gets or sets the new hotspot innkeeper.
        /// </summary>
        public Npc NewHotspotInnkeeper
        {
            get => newHotspotInnkeeper;
            set
            {
                newHotspotInnkeeper = value;
                OnPropertyChanged(nameof(NewHotspotInnkeeper));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a non-player character (NPC) that functions as a hotspot repair vendor.
        /// </summary>
        Npc newHotspotRepairVendor;
        /// <summary>
        /// Gets or sets the new hotspot repair vendor.
        /// </summary>
        public Npc NewHotspotRepairVendor
        {
            get => newHotspotRepairVendor;
            set
            {
                newHotspotRepairVendor = value;
                OnPropertyChanged(nameof(NewHotspotRepairVendor));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents an NPC that serves as a hotspot ammo vendor.
        /// </summary>
        Npc newHotspotAmmoVendor;
        /// <summary>
        /// Gets or sets the new hotspot ammo vendor.
        /// </summary>
        public Npc NewHotspotAmmoVendor
        {
            get => newHotspotAmmoVendor;
            set
            {
                newHotspotAmmoVendor = value;
                OnPropertyChanged(nameof(NewHotspotAmmoVendor));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a travel path for a new hotspot.
        /// </summary>
        TravelPath newHotspotTravelPath;
        /// <summary>
        /// Gets or sets the new hotspot travel path.
        /// </summary>
        public TravelPath NewHotspotTravelPath
        {
            get => newHotspotTravelPath;
            set
            {
                newHotspotTravelPath = value;
                OnPropertyChanged(nameof(NewHotspotTravelPath));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether a new hotspot horde exists.
        /// </summary>
        bool newHotspotHorde;
        /// <summary>
        /// Gets or sets a value indicating whether a new hotspot horde is created.
        /// </summary>
        public bool NewHotspotHorde
        {
            get => newHotspotHorde;
            set
            {
                newHotspotHorde = value;
                OnPropertyChanged(nameof(NewHotspotHorde));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether a new hotspot alliance has been formed.
        /// </summary>
        bool newHotspotAlliance;
        /// <summary>
        /// Gets or sets a value indicating whether a new hotspot alliance is created.
        /// </summary>
        public bool NewHotspotAlliance
        {
            get => newHotspotAlliance;
            set
            {
                newHotspotAlliance = value;
                OnPropertyChanged(nameof(NewHotspotAlliance));
                OnPropertyChanged(nameof(SaveHotspotCommandEnabled));
            }
        }

        /// <summary>
        /// Represents a boolean value indicating whether a new hotspot is safe for grinding.
        /// </summary>
        bool newHotspotSafeForGrinding;
        /// <summary>
        /// Gets or sets a value indicating whether the new hotspot is safe for grinding.
        /// </summary>
        public bool NewHotspotSafeForGrinding
        {
            get => newHotspotSafeForGrinding;
            set
            {
                newHotspotSafeForGrinding = value;
                OnPropertyChanged(nameof(NewHotspotSafeForGrinding));
            }
        }

        /// <summary>
        /// Gets the current state of the probe.
        /// </summary>
        // ProbeFields
        [ProbeField]
        public string CurrentState
        {
            get => probe.CurrentState;
        }

        /// <summary>
        /// Gets the current position of the probe.
        /// </summary>
        [ProbeField]
        public string CurrentPosition
        {
            get => probe.CurrentPosition;
        }

        /// <summary>
        /// Gets the current zone.
        /// </summary>
        [ProbeField]
        public string CurrentZone
        {
            get => probe.CurrentZone;
        }

        /// <summary>
        /// Gets the target name of the probe.
        /// </summary>
        [ProbeField]
        public string TargetName
        {
            get => probe.TargetName;
        }

        /// <summary>
        /// Gets the target class of the probe field.
        /// </summary>
        [ProbeField]
        public string TargetClass
        {
            get => probe.TargetClass;
        }

        /// <summary>
        /// Gets the target creature type of the probe field.
        /// </summary>
        [ProbeField]
        public string TargetCreatureType
        {
            get => probe.TargetCreatureType;
        }

        /// <summary>
        /// Gets the target position of the probe.
        /// </summary>
        [ProbeField]
        public string TargetPosition
        {
            get => probe.TargetPosition;
        }

        /// <summary>
        /// Gets the target range of the probe.
        /// </summary>
        [ProbeField]
        public string TargetRange
        {
            get => probe.TargetRange;
        }

        /// <summary>
        /// Gets the target faction ID of the probe.
        /// </summary>
        [ProbeField]
        public string TargetFactionId
        {
            get => probe.TargetFactionId;
        }

        /// <summary>
        /// Gets the value indicating whether the target is currently casting.
        /// </summary>
        [ProbeField]
        public string TargetIsCasting
        {
            get => probe.TargetIsCasting;
        }

        /// <summary>
        /// Gets the value indicating whether the target is currently channeling.
        /// </summary>
        [ProbeField]
        public string TargetIsChanneling
        {
            get => probe.TargetIsChanneling;
        }

        /// <summary>
        /// Gets the update latency of the probe field.
        /// </summary>
        [ProbeField]
        public string UpdateLatency
        {
            get => probe.UpdateLatency;
        }

        /// <summary>
        /// Gets or sets the food setting for the bot.
        /// </summary>
        // BotSettings
        [BotSetting]
        public string Food
        {
            get => botSettings.Food;
            set => botSettings.Food = value;
        }

        /// <summary>
        /// Gets or sets the drink for the bot.
        /// </summary>
        [BotSetting]
        public string Drink
        {
            get => botSettings.Drink;
            set => botSettings.Drink = value;
        }

        /// <summary>
        /// Gets or sets the included names for targeting.
        /// </summary>
        [BotSetting]
        public string TargetingIncludedNames
        {
            get => botSettings.TargetingIncludedNames;
            set => botSettings.TargetingIncludedNames = value;
        }

        /// <summary>
        /// Gets or sets the excluded names for targeting.
        /// </summary>
        [BotSetting]
        public string TargetingExcludedNames
        {
            get => botSettings.TargetingExcludedNames;
            set
            {
                botSettings.TargetingExcludedNames = value;
                OnPropertyChanged(nameof(TargetingExcludedNames));
            }
        }

        /// <summary>
        /// Gets or sets the minimum level range for the bot.
        /// </summary>
        [BotSetting]
        public int LevelRangeMin
        {
            get => botSettings.LevelRangeMin;
            set
            {
                botSettings.LevelRangeMin = value;
                OnPropertyChanged(nameof(LevelRangeMin));
            }
        }

        /// <summary>
        /// Gets or sets the maximum level range for the bot.
        /// </summary>
        [BotSetting]
        public int LevelRangeMax
        {
            get => botSettings.LevelRangeMax;
            set
            {
                botSettings.LevelRangeMax = value;
                OnPropertyChanged(nameof(LevelRangeMax));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is a beast.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeBeast
        {
            get => botSettings.CreatureTypeBeast;
            set
            {
                botSettings.CreatureTypeBeast = value;
                OnPropertyChanged(nameof(CreatureTypeBeast));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is Dragonkin.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeDragonkin
        {
            get => botSettings.CreatureTypeDragonkin;
            set
            {
                botSettings.CreatureTypeDragonkin = value;
                OnPropertyChanged(nameof(CreatureTypeDragonkin));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is demon.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeDemon
        {
            get => botSettings.CreatureTypeDemon;
            set
            {
                botSettings.CreatureTypeDemon = value;
                OnPropertyChanged(nameof(CreatureTypeDemon));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is elemental.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeElemental
        {
            get => botSettings.CreatureTypeElemental;
            set
            {
                botSettings.CreatureTypeElemental = value;
                OnPropertyChanged(nameof(CreatureTypeElemental));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is humanoid.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeHumanoid
        {
            get => botSettings.CreatureTypeHumanoid;
            set
            {
                botSettings.CreatureTypeHumanoid = value;
                OnPropertyChanged(nameof(CreatureTypeHumanoid));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is undead.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeUndead
        {
            get => botSettings.CreatureTypeUndead;
            set
            {
                botSettings.CreatureTypeUndead = value;
                OnPropertyChanged(nameof(CreatureTypeUndead));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is giant.
        /// </summary>
        [BotSetting]
        public bool CreatureTypeGiant
        {
            get => botSettings.CreatureTypeGiant;
            set
            {
                botSettings.CreatureTypeGiant = value;
                OnPropertyChanged(nameof(CreatureTypeGiant));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the unit reaction is hostile.
        /// </summary>
        [BotSetting]
        public bool UnitReactionHostile
        {
            get => botSettings.UnitReactionHostile;
            set
            {
                botSettings.UnitReactionHostile = value;
                OnPropertyChanged(nameof(UnitReactionHostile));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot's unit reaction is set to unfriendly.
        /// </summary>
        [BotSetting]
        public bool UnitReactionUnfriendly
        {
            get => botSettings.UnitReactionUnfriendly;
            set
            {
                botSettings.UnitReactionUnfriendly = value;
                OnPropertyChanged(nameof(UnitReactionUnfriendly));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot's unit reaction is neutral.
        /// </summary>
        [BotSetting]
        public bool UnitReactionNeutral
        {
            get => botSettings.UnitReactionNeutral;
            set
            {
                botSettings.UnitReactionNeutral = value;
                OnPropertyChanged(nameof(UnitReactionNeutral));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should loot poor items.
        /// </summary>
        [BotSetting]
        public bool LootPoor
        {
            get => botSettings.LootPoor;
            set
            {
                botSettings.LootPoor = value;
                OnPropertyChanged(nameof(LootPoor));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should loot common items.
        /// </summary>
        [BotSetting]
        public bool LootCommon
        {
            get => botSettings.LootCommon;
            set
            {
                botSettings.LootCommon = value;
                OnPropertyChanged(nameof(LootCommon));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should loot uncommon items.
        /// </summary>
        [BotSetting]
        public bool LootUncommon
        {
            get => botSettings.LootUncommon;
            set
            {
                botSettings.LootUncommon = value;
                OnPropertyChanged(nameof(LootUncommon));
            }
        }

        /// <summary>
        /// Gets or sets the names of items that should be excluded from looting.
        /// </summary>
        [BotSetting]
        public string LootExcludedNames
        {
            get => botSettings.LootExcludedNames;
            set
            {
                botSettings.LootExcludedNames = value;
                OnPropertyChanged(nameof(LootExcludedNames));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should sell poor items.
        /// </summary>
        [BotSetting]
        public bool SellPoor
        {
            get => botSettings.SellPoor;
            set
            {
                botSettings.SellPoor = value;
                OnPropertyChanged(nameof(SellPoor));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should sell common items.
        /// </summary>
        [BotSetting]
        public bool SellCommon
        {
            get => botSettings.SellCommon;
            set
            {
                botSettings.SellCommon = value;
                OnPropertyChanged(nameof(SellCommon));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should sell uncommon items.
        /// </summary>
        [BotSetting]
        public bool SellUncommon
        {
            get => botSettings.SellUncommon;
            set
            {
                botSettings.SellUncommon = value;
                OnPropertyChanged(nameof(SellUncommon));
            }
        }

        /// <summary>
        /// Gets or sets the excluded names for selling.
        /// </summary>
        [BotSetting]
        public string SellExcludedNames
        {
            get => botSettings.SellExcludedNames;
            set
            {
                botSettings.SellExcludedNames = value;
                OnPropertyChanged(nameof(SellExcludedNames));
            }
        }

        /// <summary>
        /// Gets or sets the Hotspot GrindingHotspot.
        /// </summary>
        [BotSetting]
        public Hotspot GrindingHotspot
        {
            get => botSettings.GrindingHotspot;
            set
            {
                botSettings.GrindingHotspot = value;
                OnPropertyChanged(nameof(GrindingHotspot));
            }
        }

        /// <summary>
        /// Gets or sets the current travel path.
        /// </summary>
        [BotSetting]
        public TravelPath CurrentTravelPath
        {
            get => botSettings.CurrentTravelPath;
            set
            {
                botSettings.CurrentTravelPath = value;
                OnPropertyChanged(nameof(CurrentTravelPath));
                OnPropertyChanged(nameof(StartTravelPathCommandEnabled));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the teleport killswitch is enabled.
        /// </summary>
        [BotSetting]
        public bool UseTeleportKillswitch
        {
            get => botSettings.UseTeleportKillswitch;
            set
            {
                botSettings.UseTeleportKillswitch = value;
                OnPropertyChanged(nameof(UseTeleportKillswitch));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the stuck in position killswitch is enabled.
        /// </summary>
        [BotSetting]
        public bool UseStuckInPositionKillswitch
        {
            get => botSettings.UseStuckInPositionKillswitch;
            set
            {
                botSettings.UseStuckInPositionKillswitch = value;
                OnPropertyChanged(nameof(UseStuckInPositionKillswitch));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the stuck in state killswitch is enabled.
        /// </summary>
        [BotSetting]
        public bool UseStuckInStateKillswitch
        {
            get => botSettings.UseStuckInStateKillswitch;
            set
            {
                botSettings.UseStuckInStateKillswitch = value;
                OnPropertyChanged(nameof(UseStuckInStateKillswitch));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should use the player targeting killswitch.
        /// </summary>
        [BotSetting]
        public bool UsePlayerTargetingKillswitch
        {
            get => botSettings.UsePlayerTargetingKillswitch;
            set
            {
                botSettings.UsePlayerTargetingKillswitch = value;
                OnPropertyChanged(nameof(UsePlayerTargetingKillswitch));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should use a player proximity killswitch.
        /// </summary>
        [BotSetting]
        public bool UsePlayerProximityKillswitch
        {
            get => botSettings.UsePlayerProximityKillswitch;
            set
            {
                botSettings.UsePlayerProximityKillswitch = value;
                OnPropertyChanged(nameof(UsePlayerProximityKillswitch));
            }
        }

        /// <summary>
        /// Gets or sets the targeting warning timer.
        /// </summary>
        [BotSetting]
        public int TargetingWarningTimer
        {
            get => botSettings.TargetingWarningTimer;
            set
            {
                botSettings.TargetingWarningTimer = value;
                OnPropertyChanged(nameof(TargetingWarningTimer));
            }
        }

        /// <summary>
        /// Gets or sets the targeting stop timer.
        /// </summary>
        [BotSetting]
        public int TargetingStopTimer
        {
            get => botSettings.TargetingStopTimer;
            set
            {
                botSettings.TargetingStopTimer = value;
                OnPropertyChanged(nameof(TargetingStopTimer));
            }
        }

        /// <summary>
        /// Gets or sets the proximity warning timer.
        /// </summary>
        [BotSetting]
        public int ProximityWarningTimer
        {
            get => botSettings.ProximityWarningTimer;
            set
            {
                botSettings.ProximityWarningTimer = value;
                OnPropertyChanged(nameof(ProximityWarningTimer));
            }
        }

        /// <summary>
        /// Gets or sets the proximity stop timer.
        /// </summary>
        [BotSetting]
        public int ProximityStopTimer
        {
            get => botSettings.ProximityStopTimer;
            set
            {
                botSettings.ProximityStopTimer = value;
                OnPropertyChanged(nameof(ProximityStopTimer));
            }
        }

        /// <summary>
        /// Gets or sets the power level player name.
        /// </summary>
        [BotSetting]
        public string PowerlevelPlayerName
        {
            get => botSettings.PowerlevelPlayerName;
            set
            {
                botSettings.PowerlevelPlayerName = value;
                OnPropertyChanged(nameof(PowerlevelPlayerName));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the travel path should be reversed.
        /// </summary>
        bool reverseTravelPath;
        /// <summary>
        /// Gets or sets a value indicating whether the travel path should be reversed.
        /// </summary>
        public bool ReverseTravelPath
        {
            get => reverseTravelPath;
            set
            {
                reverseTravelPath = value;
                OnPropertyChanged(nameof(ReverseTravelPath));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether verbose logging is enabled.
        /// </summary>
        [BotSetting]
        public bool UseVerboseLogging
        {
            get => botSettings.UseVerboseLogging;
            set
            {
                botSettings.UseVerboseLogging = value;
                OnPropertyChanged(nameof(UseVerboseLogging));
            }
        }

        /// <summary>
        /// Event that is raised when a property value changes.
        /// </summary>
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the PropertyChanged event with the specified property name.
        /// </summary>
        void OnPropertyChanged(string name) =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Logs a message to the console output with the current time.
        /// </summary>
        void Log(string message) =>
                    ConsoleOutput.Add($"({DateTime.Now.ToShortTimeString()}) {message}");

        /// <summary>
        /// Initializes the travel paths by retrieving them from the repository and setting the current travel path to null.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// InitializeTravelPaths -> ObservableCollection : Create new ObservableCollection with ListTravelPaths
        /// ObservableCollection -> InitializeTravelPaths : Return ObservableCollection
        /// InitializeTravelPaths -> ObservableCollection : Insert null at index 0
        /// InitializeTravelPaths -> OnPropertyChanged : Call with parameter "CurrentTravelPath"
        /// InitializeTravelPaths -> OnPropertyChanged : Call with parameter "TravelPaths"
        /// \enduml
        /// </remarks>
        void InitializeTravelPaths()
        {
            TravelPaths = new ObservableCollection<TravelPath>(Repository.ListTravelPaths());
            TravelPaths.Insert(0, null);
            OnPropertyChanged(nameof(CurrentTravelPath));
            OnPropertyChanged(nameof(TravelPaths));
        }

        /// <summary>
        /// Initializes the hotspots by retrieving a list of hotspots from the repository and ordering them by minimum level, zone, and description. 
        /// It then assigns the sorted hotspots to the Hotspots collection and inserts a null value at the beginning. 
        /// The GrindingHotspot is set to the first hotspot in the Hotspots collection that matches the specified GrindingHotspotId. 
        /// Finally, the OnPropertyChanged event is raised for the Hotspots property.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// InitializeHotspots -> Repository: ListHotspots()
        /// Repository --> InitializeHotspots: hotspots
        /// InitializeHotspots -> hotspots: OrderBy(h => h.MinLevel)
        /// InitializeHotspots -> hotspots: ThenBy(h => h.Zone)
        /// InitializeHotspots -> hotspots: ThenBy(h => h.Description)
        /// InitializeHotspots -> Hotspots: new ObservableCollection<Hotspot>(hotspots)
        /// InitializeHotspots -> Hotspots: Insert(0, null)
        /// InitializeHotspots -> Hotspots: FirstOrDefault(h => h?.Id == botSettings.GrindingHotspotId)
        /// InitializeHotspots -> OnPropertyChanged: nameof(Hotspots)
        /// \enduml
        /// </remarks>
        void InitializeHotspots()
        {
            var hotspots = Repository.ListHotspots()
                .OrderBy(h => h.MinLevel)
                .ThenBy(h => h.Zone)
                .ThenBy(h => h.Description);

            Hotspots = new ObservableCollection<Hotspot>(hotspots);
            Hotspots.Insert(0, null);
            GrindingHotspot = Hotspots.FirstOrDefault(h => h?.Id == botSettings.GrindingHotspotId);
            OnPropertyChanged(nameof(Hotspots));
        }

        /// <summary>
        /// Initializes the NPCs by retrieving a list of NPCs from the repository and ordering them by Horde, Innkeeper status, repairs availability, ammo selling availability, and name. 
        /// The NPCs are then stored in an ObservableCollection and null is inserted at the beginning of the collection. 
        /// Finally, the OnPropertyChanged event is raised for the Npcs property.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Repository -> InitializeNpcs: ListNpcs()
        /// InitializeNpcs -> InitializeNpcs: OrderBy(Horde)
        /// InitializeNpcs -> InitializeNpcs: ThenBy(IsInnkeeper)
        /// InitializeNpcs -> InitializeNpcs: ThenBy(Repairs)
        /// InitializeNpcs -> InitializeNpcs: ThenBy(SellsAmmo)
        /// InitializeNpcs -> InitializeNpcs: ThenBy(Name)
        /// InitializeNpcs -> Npcs: new ObservableCollection<Npc>(npcs)
        /// InitializeNpcs -> Npcs: Insert(0, null)
        /// InitializeNpcs -> InitializeNpcs: OnPropertyChanged(nameof(Npcs))
        /// \enduml
        /// </remarks>
        void InitializeNpcs()
        {
            var npcs = Repository.ListNpcs()
                .OrderBy(n => n.Horde)
                .ThenBy(n => n.IsInnkeeper)
                .ThenBy(n => n.Repairs)
                .ThenBy(n => n.SellsAmmo)
                .ThenBy(n => n.Name);

            Npcs = new ObservableCollection<Npc>(npcs);
            Npcs.Insert(0, null);
            OnPropertyChanged(nameof(Npcs));
        }

        /// <summary>
        /// Initializes the object manager by calling the Initialize method of the ObjectManager class, starting the enumeration, and initializing the command handler asynchronously.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> ObjectManager: Initialize(probe)
        /// ObjectManager -> ObjectManager: StartEnumeration()
        /// ObjectManager -> Task: Run(InitializeCommandHandler)
        /// \enduml
        /// </remarks>
        public void InitializeObjectManager()
        {
            ObjectManager.Initialize(probe);
            ObjectManager.StartEnumeration();
            Task.Run(async () => await InitializeCommandHandler());
        }

        /// <summary>
        /// Updates the properties of the current object that have the specified attribute.
        /// </summary>
        /// <param name="type">The type of attribute to search for.</param>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// UpdatePropertiesWithAttribute -> GetType : Get properties of current type
        /// GetType --> UpdatePropertiesWithAttribute : Return properties
        /// loop for each property
        ///     UpdatePropertiesWithAttribute -> Attribute : Check if attribute is defined
        ///     Attribute --> UpdatePropertiesWithAttribute : Return attribute status
        ///     alt if attribute is defined
        ///         UpdatePropertiesWithAttribute -> OnPropertyChanged : Invoke with property name
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        void UpdatePropertiesWithAttribute(Type type)
        {
            foreach (var propertyInfo in GetType().GetProperties())
            {
                if (Attribute.IsDefined(propertyInfo, type))
                    OnPropertyChanged(propertyInfo.Name);
            }
        }

        /// <summary>
        /// Callback method for handling chat messages.
        /// Sends a message to the Discord client if the player is not null and the current zone is not in the list of city names.
        /// The message content depends on the chat channel.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// object "sender" as Sender
        /// object "OnChatMessageArgs e" as Args
        /// object "ObjectManager.Player" as Player
        /// object "DiscordClientWrapper" as Discord
        ///
        /// Sender -> Args : Chat Message
        /// Args -> Player : Check Player and Zone
        /// note right of Player: If player is not null and not in city
        /// Args -> Discord : Send Message
        /// note right of Discord: If channel is 'Say' or 'Whisper'
        /// \enduml
        /// </remarks>
        void OnChatMessageCallback(object sender, OnChatMessageArgs e)
        {
            var player = ObjectManager.Player;
            if (player != null && !CityNames.Contains(ObjectManager.ZoneText))
            {
                if (e.ChatChannel == "Say")
                    DiscordClientWrapper.SendMessage($"{player.Name} saw a chat message from {e.UnitName}: {e.Message}");
                else if (e.ChatChannel == "Whisper")
                    DiscordClientWrapper.SendMessage($"{player.Name} received a whisper from {e.UnitName}: {e.Message}");
            }
        }

        /// <summary>
        /// Initializes the command handler.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// InitializeCommandHandler -> WoWEventHandler: OnChatMessage += OnChatMessageCallback
        /// InitializeCommandHandler -> ObjectManager: Get Player
        /// ObjectManager --> InitializeCommandHandler: Return Player
        /// InitializeCommandHandler -> Repository: DeleteCommandsForPlayer(player.Name)
        /// InitializeCommandHandler -> InitializeCommandHandler: SignLatestReport(player, false)
        /// InitializeCommandHandler -> InitializeCommandHandler: readyForCommands = true
        /// InitializeCommandHandler -> InitializeCommandHandler: await Task.Delay(2000)
        /// InitializeCommandHandler -> Repository: GetCommandsForPlayer(ObjectManager.Player.Name)
        /// Repository --> InitializeCommandHandler: Return Commands
        /// InitializeCommandHandler -> InitializeCommandHandler: Process Commands
        /// InitializeCommandHandler -> ThreadSynchronizer: RunOnMainThread
        /// ThreadSynchronizer --> InitializeCommandHandler: Execute LuaCall
        /// InitializeCommandHandler -> Repository: DeleteCommand(command.Id)
        /// InitializeCommandHandler -> InitializeCommandHandler: SignLatestReport(player, true)
        /// InitializeCommandHandler -> Logger: Log(e)
        /// InitializeCommandHandler -> InitializeCommandHandler: await Task.Delay(250)
        /// \enduml
        /// </remarks>
        async Task InitializeCommandHandler()
        {
            WoWEventHandler.OnChatMessage += OnChatMessageCallback;

            while (true)
            {
                try
                {
                    var player = ObjectManager.Player;

                    if (player != null)
                    {
                        if (!readyForCommands)
                        {
                            if (string.IsNullOrWhiteSpace(player.Name) || player.Level == 0)
                                continue;

                            Repository.DeleteCommandsForPlayer(player.Name);
                            SignLatestReport(player, false);
                            readyForCommands = true;

                            await Task.Delay(2000); // wait for 1 second to sign the latest report, otherwise you'll randomly report on login
                        }
                        else
                        {
                            var commands = Repository.GetCommandsForPlayer(ObjectManager.Player.Name);

                            foreach (var command in commands)
                            {
                                switch (command.Command)
                                {
                                    case "!start":
                                        Start();
                                        break;
                                    case "!stop":
                                        Stop();
                                        break;
                                    case "!chat":
                                        ThreadSynchronizer.RunOnMainThread(() =>
                                        {
                                            player.LuaCall($"SendChatMessage('{command.Args}')");
                                        });
                                        break;
                                    case "!whisper":
                                        ThreadSynchronizer.RunOnMainThread(() =>
                                        {
                                            var splitArgs = command.Args.Split(' ');
                                            var recipient = splitArgs[0];
                                            var message = string.Join(" ", splitArgs.Skip(1));

                                            player.LuaCall($"SendChatMessage('{message}', 'WHISPER', nil, '{recipient}')");
                                        });
                                        break;
                                    case "!logout":
                                        ThreadSynchronizer.RunOnMainThread(() =>
                                        {
                                            player.LuaCall("Logout()");
                                        });
                                        break;
                                    case "!hearthstone":
                                        ThreadSynchronizer.RunOnMainThread(() =>
                                        {
                                            var hearthstone = Inventory.GetAllItems().FirstOrDefault(i => i.Info.Name == "Hearthstone");
                                            if (hearthstone != null)
                                                hearthstone.Use();
                                        });
                                        break;
                                    case "!status":
                                        ThreadSynchronizer.RunOnMainThread(() =>
                                        {
                                            string[] hordeRaces = { "Orc", "Troll", "Tauren", "Undead" };
                                            string[] aRaces = { "Human", "Dwarf", "Night elf", "Gnome", "Tauren", "Troll" };
                                            var status = new StringBuilder();
                                            var race = player.LuaCallWithResults("{0} = UnitRace('player')")[0];
                                            var raceString = aRaces.Contains(race) ? $"a {race}" : $"an {race}";
                                            var alive = player.Health <= 0 || player.InGhostForm ? "dead" : "alive";
                                            var greeting = hordeRaces.Contains(race) ? "Zug zug!" : "Hail, and well met!";
                                            status.Append($"{greeting} {player.Name} reporting in. I'm {raceString} {player.Class} on the server {ObjectManager.ServerName}.\n");
                                            status.Append($"I'm currently level {player.Level}, and I'm {alive}!\n");
                                            if (CurrentBot.Running())
                                            {
                                                status.Append($"I'm currently in the {probe.CurrentState}.\n");
                                                status.Append($"I'm grinding in {GrindingHotspot.DisplayName}.\n");
                                            }
                                            else
                                            {
                                                status.Append("I'm currently idle.");
                                            }
                                            DiscordClientWrapper.SendMessage(status.ToString());
                                        });
                                        break;
                                    case "!info":
                                        var dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(MainViewModel)).CodeBase);
                                        var path = new UriBuilder(dir).Path;
                                        var bloogBotExe = $"{path}\\BloogBot.exe";
                                        var bloogBotExeAssemblyVersion = AssemblyName.GetAssemblyName(bloogBotExe).Version;
                                        var botDll = $"{path}\\{CurrentBot.FileName}";
                                        var botAssemblyVersion = AssemblyName.GetAssemblyName(botDll).Version;
                                        var sb = new StringBuilder();
                                        sb.Append($"{player.Name}\n");
                                        sb.Append($"BloogBot.exe version {bloogBotExeAssemblyVersion}\n");
                                        sb.Append($"{CurrentBot.FileName} version {botAssemblyVersion}\n");
                                        DiscordClientWrapper.SendMessage(sb.ToString());
                                        break;
                                }

                                Repository.DeleteCommand(command.Id);
                            }

                            SignLatestReport(player, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(e);
                }

                await Task.Delay(250);
            }
        }

        /// <summary>
        /// Signs the latest report for the specified player.
        /// </summary>
        /// <param name="player">The player to sign the report for.</param>
        /// <param name="reportIn">A flag indicating whether the player is reporting in.</param>
        /// <remarks>
        /// \startuml
        /// LocalPlayer -> SignLatestReport: Call
        /// SignLatestReport -> Repository: GetLatestReportSignatures
        /// Repository --> SignLatestReport: Return summary
        /// SignLatestReport -> CurrentBot: Running
        /// CurrentBot --> SignLatestReport: Return status
        /// SignLatestReport -> DiscordClientWrapper: SendMessage
        /// DiscordClientWrapper --> SignLatestReport: Acknowledge
        /// SignLatestReport -> Repository: AddReportSignature
        /// Repository --> SignLatestReport: Acknowledge
        /// \enduml
        /// </remarks>
        void SignLatestReport(LocalPlayer player, bool reportIn)
        {
            var summary = Repository.GetLatestReportSignatures();

            if (summary.CommandId != -1 && !summary.Signatures.Any(s => s.Player == player.Name))
            {
                if (reportIn)
                {
                    var active = CurrentBot.Running() ? $"Grinding at: {GrindingHotspot.DisplayName}" : "Idle";
                    DiscordClientWrapper.SendMessage($"{player.Name} ({player.Class} - Level {player.Level}). Server: {ObjectManager.ServerName}. {active}.");
                }

                Repository.AddReportSignature(player.Name, summary.CommandId);
            }
        }
    }

    /// <summary>
    /// Represents an attribute used to define settings for a bot.
    /// </summary>
    public class BotSettingAttribute : Attribute
    {
    }

    /// <summary>
    /// Represents an attribute used to mark a field as a probe field.
    /// </summary>
    public class ProbeFieldAttribute : Attribute
    {
    }
}