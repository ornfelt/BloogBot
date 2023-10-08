﻿using BloogBot.Game;
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
        readonly LocalPlayer player;
        private QueueStates currentState;
        private bool otherCTA, avCTA, abCTA;
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
            if (currentState == QueueStates.Initial)
            {
                player.StopAllMovement();
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameOpened;
                return;
            }

            if (currentState == QueueStates.PVPFrameOpened && Wait.For("QueueDelay", 1500))
            {
                player.LuaCall($"PVPParentFrameTab2:Click()");
                currentState = QueueStates.PVPTabOpened;
                return;
            }

            if (currentState == QueueStates.PVPTabOpened && Wait.For("QueueDelay", 1500))
            {
                player.LuaCall($"PVPBattlegroundFrame.selectedBG = {RandBgQueueIndex()}");
                currentState = QueueStates.BgChosen;
                return;
            }

            if (currentState == QueueStates.BgChosen && Wait.For("QueueDelay", 1500))
            {
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameToggled;
                return;
            }

            if (currentState == QueueStates.PVPFrameToggled && Wait.For("QueueDelay", 1500))
            {
                player.LuaCall($"TogglePVPFrame()");
                currentState = QueueStates.PVPFrameToggledAgain;
                return;
            }

            if (currentState == QueueStates.PVPFrameToggledAgain && Wait.For("QueueDelay", 1500))
            {
                player.LuaCall($"JoinBattlefield(0,0)");
                currentState = QueueStates.Queued;
                return;
            }

            if (currentState == QueueStates.Queued && Wait.For("QueueDelay", 5000))
            {
                player.LuaCall($"StaticPopup1Button1:Click()");
                player.HasJoinedBg = true;
                player.HasEnteredNewMap = true;
                botStates.Pop();
                return;
            }
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
            int bg = rand.Next(3);
            int bgQueueIndex = bg;

            if (bg == 0)
            {
                // WSG
                if (otherCTA || abCTA || avCTA)
                    bgQueueIndex = 3;
                else
                    bgQueueIndex = 2;
            }
            else if (bg == 1)
            {
                // AB
                if (otherCTA || avCTA)
                    bgQueueIndex = 3;
                else if (abCTA)
                    bgQueueIndex = 2;
                else
                    bgQueueIndex = 3;
            }
            else
            {
                // AV
                if (otherCTA)
                    bgQueueIndex = 5;
                else if (avCTA)
                    bgQueueIndex = 2;
                else
                    bgQueueIndex = 4;
            }
            // If low level
            if (player.Level < 70)
            {
                if (bg == 0)
                    bgQueueIndex = 1;
                else if (bg == 1)
                {
                    if (abCTA)
                        bgQueueIndex = 1;
                    else if (avCTA)
                        bgQueueIndex = 3;
                    else
                        bgQueueIndex = 2;
                }
                else
                {
                    if (avCTA)
                        bgQueueIndex = 1;
                    else
                        bgQueueIndex = 3;
                }
            }

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
            bool eyeCTA = CheckCTA("2010-04-30 18:00:00", occurence, length);
            // Strand: 400
            bool strandCTA = CheckCTA("2010-04-09 18:00:00", occurence, length);
            // Isle: 420
            bool isleCTA = CheckCTA("2010-04-16 18:00:00", occurence, length);

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
        Queued,
    }
}