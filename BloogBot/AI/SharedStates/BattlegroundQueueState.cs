using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace BloogBot.AI.SharedStates
{
    public class BattlegroundQueueState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        LocalPlayer player;
        private QueueStates currentState;
        private bool otherCTA, eyeCTA, strandCTA, isleCTA, avCTA, abCTA;
        private static Random rand = new Random();

        public BattlegroundQueueState(
            Stack<IBotState> botStates,
            IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

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
                player.LuaCall($"PVPBattlegroundFrame.selectedBG = {RandBgQueueIndex()}");
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

        private bool CheckCombat()
        {
            if (player.IsInCombat)
            {
                botStates.Pop();
                botStates.Push(new GrindState(botStates, container));
                return true;
            }
            return false;
        }

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

        private int RandBgQueueIndex()
        {
            SetCTA();
            int playerLevel = player.Level;
            int bg = (playerLevel < 20) ? 0 : (playerLevel < 51) ? rand.Next(2) : rand.Next(3);

            int bgQueueIndex = bg;

            if (playerLevel < 20)
                bgQueueIndex = 2;
            else if (playerLevel < 51)
                bgQueueIndex = (bg == 0 && !abCTA) || (bg == 1 && abCTA) ? 2 : 3;
            else if (playerLevel < 61)
                bgQueueIndex = (bg == 0 && !abCTA && !avCTA) ? 2 : (bg == 1 && avCTA) || (bg == 2 && avCTA) ? 4 : 3;
            else if (playerLevel < 71)
                bgQueueIndex = (bg == 0 && !abCTA && !avCTA && !eyeCTA) ? 2 : (bg == 1 && (avCTA || eyeCTA)) || (bg == 2 && avCTA) ? 4 : 3;
            else
                bgQueueIndex = bg == 0 ? (otherCTA || abCTA || avCTA ? 3 : 2) :
                       bg == 1 ? (otherCTA || avCTA ? 4 : abCTA ? 2 : 3) :
                                 (otherCTA ? 5 : avCTA ? 2 : 4);

            Console.WriteLine($"Queueing for bg: {bg}, bgQueueIndex: {bgQueueIndex}");
            return bgQueueIndex;
        }

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
