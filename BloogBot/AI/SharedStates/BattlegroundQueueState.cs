using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains the shared states for the AI in battleground queue.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a class that manages the state of the battleground queue for a bot.
    /// </summary>
    /// <summary>
    /// Represents a class that manages the state of the battleground queue for a bot.
    /// </summary>
    public class BattlegroundQueueState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a local player.
        /// </summary>
        LocalPlayer player;
        /// <summary>
        /// Represents the current state of the queue.
        /// </summary>
        private QueueStates currentState;
        /// <summary>
        /// Represents the boolean variables for otherCTA, eyeCTA, strandCTA, isleCTA, avCTA, and abCTA.
        /// </summary>
        private bool otherCTA, eyeCTA, strandCTA, isleCTA, avCTA, abCTA;
        /// <summary>
        /// Represents a static random number generator.
        /// </summary>
        private static Random rand = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="BattlegroundQueueState"/> class.
        /// </summary>
        public BattlegroundQueueState(
                    Stack<IBotState> botStates,
                    IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the current state of the player's PVP queue.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> Player: Get Player
        /// Player -> QueueStates: Check Initial State
        /// QueueStates --> Player: StopAllMovement
        /// Player -> QueueStates: Change State to PVPFrameOpened
        /// QueueStates -> Player: Check PVPFrameOpened State
        /// QueueStates --> Player: LuaCall PVPParentFrameTab2:Click
        /// Player -> QueueStates: Change State to PVPTabOpened
        /// QueueStates -> Player: Check PVPTabOpened State
        /// QueueStates --> Player: LuaCall for i=1,GetNumBattlegroundTypes
        /// Player -> QueueStates: Change State to BgChosen
        /// QueueStates -> Player: Check BgChosen State
        /// QueueStates --> Player: LuaCall TogglePVPFrame
        /// Player -> QueueStates: Change State to PVPFrameToggled
        /// QueueStates -> Player: Check PVPFrameToggled State
        /// QueueStates --> Player: LuaCall TogglePVPFrame
        /// Player -> QueueStates: Change State to PVPFrameToggledAgain
        /// QueueStates -> Player: Check PVPFrameToggledAgain State
        /// QueueStates --> Player: LuaCall JoinBattlefield(0,0)
        /// Player -> QueueStates: Change State to Queued
        /// QueueStates -> Player: Check Queued State
        /// QueueStates --> Player: LuaCall AcceptBattlefieldPort(1,1)
        /// Player -> BotStates: Pop
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (CheckCombat())
                return;
            player = ObjectManager.Player;

            if (currentState == QueueStates.Initial)
            {
                player.StopAllMovement();
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameOpened;
                return;
            }

            if (currentState == QueueStates.PVPFrameOpened && Wait.For("PVPFrameOpenedDelay", 1500))
            {
                player.LuaCall($"PVPParentFrameTab2:Click()");
                currentState = QueueStates.PVPTabOpened;
                return;
            }

            if (currentState == QueueStates.PVPTabOpened && Wait.For("PVPTabOpenedDelay", 1500))
            {
                // player.LuaCall($"PVPBattlegroundFrame.selectedBG = {RandBgQueueIndex()}"); // Old
                // Guaranteed correct queue index
                Dictionary<int, string> battlegroundNames = new Dictionary<int, string>
                {
                    { 0, "Warsong Gulch" },
                    { 1, "Arathi Basin" },
                    { 2, "Alterac Valley" }
                };
                player.LuaCall($"for i=1,GetNumBattlegroundTypes()do local name,_,_,_,_=GetBattlegroundInfo(i)if name=='{battlegroundNames[RandBgQueueIndex()]}'then PVPBattlegroundFrame.selectedBG = i end end");
                // You can also just JoinBattlefield directly:
                //run for i=1,GetNumBattlegroundTypes()do local name,_,_,_,_=GetBattlegroundInfo(i)if name=="Warsong Gulch"then JoinBattlefield(i)end end

                currentState = QueueStates.BgChosen;
                return;
            }

            if (currentState == QueueStates.BgChosen && Wait.For("BgChosenDelay", 1500))
            {
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameToggled;
                return;
            }

            if (currentState == QueueStates.PVPFrameToggled && Wait.For("PVPFrameToggledDelay", 1500))
            {
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameToggledAgain;
                return;
            }

            if (currentState == QueueStates.PVPFrameToggledAgain && Wait.For("PVPFrameToggledAgainDelay", 1500))
            {
                player.LuaCall($"JoinBattlefield(0,0)");
                currentState = QueueStates.Queued;
                return;
            }

            if (currentState == QueueStates.Queued && Wait.For("QueuedDelay", 5000))
            {
                //player.LuaCall($"StaticPopup1Button1:Click()");
                //player.LuaCall($"StaticPopup1Button1:Click(LeftButton, true)");
                player.LuaCall($"AcceptBattlefieldPort(1,1)");
                player.HasJoinedBg = true;
                botStates.Pop();
                return;
            }
        }

        /// <summary>
        /// Checks if the player is in combat. If the player is in combat, it pops the current bot state and pushes a new GrindState onto the bot state stack. Returns true if the player is in combat, otherwise returns false.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CheckCombat -> player: IsInCombat
        /// alt IsInCombat is true
        ///     CheckCombat -> player: LuaCall("TogglePVPFrame")
        ///     CheckCombat -> botStates: Pop
        ///     CheckCombat -> botStates: Push(new GrindState)
        ///     CheckCombat --> : return true
        /// else IsInCombat is false
        ///     CheckCombat --> : return false
        /// end
        /// \enduml
        /// </remarks>
        private bool CheckCombat()
        {
            if (player.IsInCombat)
            {
                if (currentState == QueueStates.PVPTabOpened || currentState == QueueStates.PVPFrameToggledAgain)
                    player.LuaCall($"TogglePVPFrame()");
                botStates.Pop();
                botStates.Push(new GrindState(botStates, container));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the current time is within the specified occurrence and length based on the start time.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CheckCTA Method" as CTA
        /// participant "DateTime" as DT
        /// participant "TimeSpan" as TS
        /// CTA -> DT: ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null)
        /// activate DT
        /// DT --> CTA: Return startTimeDate
        /// deactivate DT
        /// CTA -> CTA: Calculate difference (DateTime.Now - startTimeDate)
        /// CTA -> TS: TotalSeconds
        /// activate TS
        /// TS --> CTA: Return differenceInSeconds
        /// deactivate TS
        /// CTA -> CTA: Check if (differenceInSeconds % (occurence * MINUTES)) < (length * MINUTES)
        /// CTA --> CTA: Return result
        /// \enduml
        /// </remarks>
        private bool CheckCTA(string startTime, long occurence, long length)
        {
            // Convert the startTime string to a DateTime object
            DateTime startTimeDate = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null);
            // Calculate the difference between the current time and the start time
            TimeSpan difference = DateTime.Now - startTimeDate;
            // Convert the difference to seconds
            double differenceInSeconds = difference.TotalSeconds;
            const int MINUTES = 60;
            // Check if the current time is within the occurence and length
            return (differenceInSeconds % (occurence * MINUTES)) < (length * MINUTES);
        }

        /// <summary>
        /// Generates a random background queue index based on the player's level and certain conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate RandBgQueueIndex
        /// RandBgQueueIndex -> SetCTA: SetCTA()
        /// RandBgQueueIndex -> player: Get Level
        /// RandBgQueueIndex -> RandBgQueueIndex: Calculate bg
        /// RandBgQueueIndex -> RandBgQueueIndex: Calculate bgQueueIndex
        /// RandBgQueueIndex -> Console: WriteLine
        /// deactivate RandBgQueueIndex
        /// \enduml
        /// </remarks>
        private int RandBgQueueIndex()
        {
            SetCTA();
            int playerLevel = player.Level;
            int bg = (playerLevel < 20) ? 0 : (playerLevel < 51) ? rand.Next(2) : rand.Next(3);

            int bgQueueIndex = bg;

            // This works 90% of the time
            if (playerLevel < 20)
                bgQueueIndex = 2;
            else if (playerLevel < 51)
                bgQueueIndex = (bg == 0 && !abCTA) || (bg == 1 && abCTA) ? 2 : 3;
            else if (playerLevel < 61)
                bgQueueIndex = bg == 0 ? (!abCTA && !avCTA ? 2 : 3) :
                       bg == 1 ? (abCTA ? 2 : 3) :
                                 (avCTA ? 2 : 4);
            else if (playerLevel < 71)
                bgQueueIndex = bg == 0 ? (!abCTA && !avCTA && !eyeCTA ? 2 : 3) :
                       bg == 1 ? (abCTA ? 2 : (eyeCTA || avCTA ? 4 : 3)) :
                                 (avCTA ? 2 : (eyeCTA ? 5 : 4));
            else
                bgQueueIndex = bg == 0 ? (otherCTA || abCTA || avCTA ? 3 : 2) :
                       bg == 1 ? (otherCTA || avCTA ? 4 : abCTA ? 2 : 3) :
                                 (otherCTA ? 5 : avCTA ? 2 : 4);

            Console.WriteLine($"Queueing for bg: {bg}, bgQueueIndex: {bgQueueIndex}");
            return bg;
            //return bgQueueIndex;
        }

        /// <summary>
        /// Sets the current Call to Arms for various game events.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// SetCTA -> CheckCTA: "2010-05-07 18:00:00", occurence, length
        /// SetCTA -> CheckCTA: "2010-04-02 18:00:00", occurence, length
        /// SetCTA -> CheckCTA: "2010-04-23 18:00:00", occurence, length
        /// SetCTA -> CheckCTA: "2010-04-30 18:00:00", occurence, length
        /// SetCTA -> CheckCTA: "2010-04-09 18:00:00", occurence, length
        /// SetCTA -> CheckCTA: "2010-04-16 18:00:00", occurence, length
        /// SetCTA --> Console: abCTA, avCTA, otherCTA
        /// \enduml
        /// </remarks>
        void SetCTA()
        {
            // Calculate current call to arms
            // select * from game_event where holiday in (283, 284, 285, 353, 400, 420);
            // The start dates could be fetched through SQL if needed...
            long occurence = 60480;
            long length = 6240;

            // AV: 283
            avCTA = CheckCTA("2010-05-07 18:00:00", occurence, length);
            // WSG: 284
            bool wsgCta = CheckCTA("2010-04-02 18:00:00", occurence, length);
            // AB: 285
            abCTA = CheckCTA("2010-04-23 18:00:00", occurence, length);
            // EYE: 353
            eyeCTA = CheckCTA("2010-04-30 18:00:00", occurence, length);
            // Strand: 400
            strandCTA = CheckCTA("2010-04-09 18:00:00", occurence, length);
            // Isle: 420
            isleCTA = CheckCTA("2010-04-16 18:00:00", occurence, length);

            otherCTA = (eyeCTA || strandCTA || isleCTA);
            Console.WriteLine($"abCTA: {abCTA}, avCTA: {avCTA}, otherCTA: {otherCTA}");
        }
    }

    /// <summary>
    /// Represents the possible states of a queue.
    /// </summary>
    enum QueueStates
    {
        Initial,
        PVPFrameOpened,
        PVPTabOpened,
        BgChosen,
        PVPFrameToggled,
        PVPFrameToggledAgain,
        Queued
    }
}
