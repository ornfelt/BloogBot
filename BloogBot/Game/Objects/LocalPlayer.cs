using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BloogBot.Game.Objects
{
    public class LocalPlayer : WoWPlayer
    {
        internal LocalPlayer(
            IntPtr pointer,
            ulong guid,
            ObjectType objectType)
            : base(pointer, guid, objectType)
        {
            RefreshSpells();
        }

        readonly Random random = new Random();

        // WARRIOR
        const string BattleStance = "Battle Stance";
        const string BerserkerStance = "Berserker Stance";
        const string DefensiveStance = "Defensive Stance";

        // DRUID
        const string BearForm = "Bear Form";
        const string CatForm = "Cat Form";

        // OPCODES
        const int SET_FACING_OPCODE = 0xDA; // TBC

        public readonly IDictionary<string, int[]> PlayerSpells = new Dictionary<string, int[]>();

        public WoWUnit Target { get; set; }

        bool turning;
        int totalTurns;
        int turnCount;
        float amountPerTurn;
        Position turningToward;

        public Class Class => (Class)MemoryManager.ReadByte((IntPtr)MemoryAddresses.LocalPlayerClass);

        public void AntiAfk() => MemoryManager.WriteInt((IntPtr)MemoryAddresses.LastHardwareAction, Environment.TickCount);

        public Position CorpsePosition => new Position(
            MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionX),
            MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionY),
            MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionZ));

        public void Face(Position pos)
        {
            // sometimes the client gets in a weird state and CurrentFacing is negative. correct that here.
            if (Facing < 0)
            {
                SetFacing((float)(Math.PI * 2) + Facing);
                return;
            }

            // if this is a new position, restart the turning flow
            if (turning && pos != turningToward)
            {
                ResetFacingState();
                return;
            }

            // return if we're already facing the position
            if (!turning && IsFacing(pos))
                return;

            if (!turning)
            {
                var requiredFacing = GetFacingForPosition(pos);
                float amountToTurn;
                if (requiredFacing > Facing)
                {
                    if (requiredFacing - Facing > Math.PI)
                    {
                        amountToTurn = -((float)(Math.PI * 2) - requiredFacing + Facing);
                    }
                    else
                    {
                        amountToTurn = requiredFacing - Facing;
                    }
                }
                else
                {
                    if (Facing - requiredFacing > Math.PI)
                    {
                        amountToTurn = (float)(Math.PI * 2) - Facing + requiredFacing;
                    }
                    else
                    {
                        amountToTurn = requiredFacing - Facing;
                    }
                }

                // if the turn amount is relatively small, just face that direction immediately
                if (Math.Abs(amountToTurn) < 0.05)
                {
                    SetFacing(requiredFacing);
                    ResetFacingState();
                    return;
                }

                turning = true;
                turningToward = pos;
                totalTurns = random.Next(2, 5);
                amountPerTurn = amountToTurn / totalTurns;
            }
            if (turning)
            {
                if (turnCount < totalTurns - 1)
                {
                    var twoPi = (float)(Math.PI * 2);
                    var newFacing = Facing + amountPerTurn;

                    if (newFacing < 0)
                        newFacing = twoPi + amountPerTurn + Facing;
                    else if (newFacing > Math.PI * 2)
                        newFacing = amountPerTurn - (twoPi - Facing);

                    SetFacing(newFacing);
                    turnCount++;
                }
                else
                {
                    SetFacing(GetFacingForPosition(pos));
                    ResetFacingState();
                }
            }
        }

        // Nat added this to see if he could test out the cleave radius which is larger than that isFacing radius
        public bool IsInCleave(Position position) => Math.Abs(GetFacingForPosition(position) - Facing) < 3f;

        public void SetFacing(float facing)
        {
            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
            {
                Functions.SetFacing(Pointer, facing);
            }
            else
            {
                Functions.SetFacing(IntPtr.Add(Pointer, MemoryAddresses.LocalPlayer_SetFacingOffset), facing);
                Functions.SendMovementUpdate(Pointer, SET_FACING_OPCODE);
            }
        }

        public void MoveToward(Position pos)
        {
            Face(pos);
            StartMovement(ControlBits.Front);
        }

        void ResetFacingState()
        {
            turning = false;
            totalTurns = 0;
            turnCount = 0;
            amountPerTurn = 0;
            turningToward = null;
            StopMovement(ControlBits.StrafeLeft);
            StopMovement(ControlBits.StrafeRight);
        }

        public void Turn180()
        {
            var newFacing = Facing + Math.PI;
            if (newFacing > (Math.PI * 2))
                newFacing -= Math.PI * 2;
            SetFacing((float)newFacing);
        }

        // the client will NOT send a packet to the server if a key is already pressed, so you're safe to spam this
        public void StartMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Logger.LogVerbose($"StartMovement: {bits}");

            Functions.SetControlBit((int)bits, 1, Environment.TickCount);
        }

        public void StopAllMovement()
        {
            var bits = ControlBits.Front | ControlBits.Back | ControlBits.Left | ControlBits.Right | ControlBits.StrafeLeft | ControlBits.StrafeRight;

            StopMovement(bits);
        }

        public void StopMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Logger.LogVerbose($"StopMovement: {bits}");
            Functions.SetControlBit((int)bits, 0, Environment.TickCount);
        }

        public void Jump()
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                StopMovement(ControlBits.Jump);
                StartMovement(ControlBits.Jump);
            }
            else
            {
                Functions.Jump();
            }
        }

        // use this to determine whether you can use cannibalize
        public bool TastyCorpsesNearby =>
            ObjectManager.Units.Any(u =>
                u.Position.DistanceTo(Position) < 5
                && u.CreatureType.HasFlag(CreatureType.Humanoid | CreatureType.Undead)
            );

        public void Stand() => LuaCall("DoEmote(\"STAND\")");

        public string CurrentStance
        {
            get
            {
                if (Buffs.Any(b => b.Name == BattleStance))
                    return BattleStance;

                if (Buffs.Any(b => b.Name == DefensiveStance))
                    return DefensiveStance;

                if (Buffs.Any(b => b.Name == BerserkerStance))
                    return BerserkerStance;

                return "None";
            }
        }

        public bool InGhostForm
        {
            get
            {
                var result = LuaCallWithResults($"{{0}} = UnitIsGhost('player')");

                if (result.Length > 0)
                    return result[0] == "1";
                else
                    return false;
            }
        }

        public void SetTarget(ulong guid) => Functions.SetTarget(guid);

        ulong ComboPointGuid { get; set; }
        public bool CanOverpower
        {
            get
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    return MemoryManager.ReadInt((IntPtr)MemoryAddresses.LocalPlayerCanOverpower) > 0;
                }
                else
                {
                    var ptr1 = MemoryManager.ReadIntPtr(Pointer + 0xE68);
                    var ptr2 = IntPtr.Add(ptr1, 0x1029);
                    if (ComboPointGuid == 0)
                        MemoryManager.WriteBytes(ptr2, new byte[] { 0 });
                    var points = MemoryManager.ReadByte(ptr2);
                    if (points == 0)
                    {
                        ComboPointGuid = TargetGuid;
                        return false;
                    }
                    if (ComboPointGuid != TargetGuid)
                    {
                        MemoryManager.WriteBytes(ptr2, new byte[] { 0 });
                        return false;
                    }
                    return MemoryManager.ReadByte(ptr2) > 0;
                }
            }
        }

        public byte ComboPoints
        {
            get
            {
                var result = ObjectManager.Player.LuaCallWithResults($"{{0}} = GetComboPoints('target')");

                if (result.Length > 0)
                    return Convert.ToByte(result[0]);
                else
                    return 0;
            }
        }

        public string CurrentShapeshiftForm
        {
            get
            {
                if (HasBuff(BearForm))
                    return BearForm;

                if (HasBuff(CatForm))
                    return CatForm;

                return "Human Form";
            }
        }

        public bool IsDiseased => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Disease);

        public bool IsCursed => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Curse);

        public bool IsPoisoned => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Poison);

        public bool HasMagicDebuff => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Magic);

        public void ReleaseCorpse() => Functions.ReleaseCorpse(Pointer);

        public void RetrieveCorpse() => Functions.RetrieveCorpse();

        public void RefreshSpells()
        {
            PlayerSpells.Clear();
            for (var i = 0; i < 1024; i++)
            {
                var currentSpellId = MemoryManager.ReadInt((IntPtr)(MemoryAddresses.LocalPlayerSpellsBase + 4 * i));
                if (currentSpellId == 0) break;

                string name;
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var spellsBasePtr = MemoryManager.ReadIntPtr((IntPtr)0x00C0D788);
                    var spellPtr =  MemoryManager.ReadIntPtr(spellsBasePtr + currentSpellId * 4);

                    var spellNamePtr = MemoryManager.ReadIntPtr(spellPtr + 0x1E0);
                    name = MemoryManager.ReadString(spellNamePtr);

                    if (PlayerSpells.ContainsKey(name))
                        PlayerSpells[name] = new List<int>(PlayerSpells[name])
                    {
                        currentSpellId
                    }.ToArray();
                    else
                        PlayerSpells.Add(name, new[] { currentSpellId });
                }
                else
                {
                    name = Functions.GetSpellDBEntry(currentSpellId).Name;
                }

                if (PlayerSpells.ContainsKey(name))
                    PlayerSpells[name] = new List<int>(PlayerSpells[name])
                    {
                        currentSpellId
                    }.ToArray();
                else
                    PlayerSpells.Add(name, new[] { currentSpellId });
            }
        }

        public int GetSpellId(string spellName, int rank = -1)
        {
            int spellId;

            var maxRank = PlayerSpells[spellName].Length;
            if (rank < 1 || rank > maxRank)
                spellId = PlayerSpells[spellName][maxRank - 1];
            else
                spellId = PlayerSpells[spellName][rank - 1];

            return spellId;
        }

        public bool IsSpellReady(string spellName, int rank = -1)
        {
            if (!PlayerSpells.ContainsKey(spellName))
                return false;

            var spellId = GetSpellId(spellName, rank);

            return !Functions.IsSpellOnCooldown(spellId);
        }

        public int GetManaCost(string spellName, int rank = -1)
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                var parId = GetSpellId(spellName, rank);

                if (parId >= MemoryManager.ReadUint((IntPtr)(0x00C0D780 + 0xC)) || parId <= 0)
                    return 0;

                var entryPtr = MemoryManager.ReadIntPtr((IntPtr)((uint)(MemoryManager.ReadUint((IntPtr)(0x00C0D780 + 8)) + parId * 4)));
                return MemoryManager.ReadInt((entryPtr + 0x0080));
            }
            else
            {
                var spellId = GetSpellId(spellName, rank);
                return Functions.GetSpellDBEntry(spellId).Cost;
            }
        }

        public bool KnowsSpell(string name) => PlayerSpells.ContainsKey(name);

        public bool MainhandIsEnchanted => LuaCallWithResults("{0} = GetWeaponEnchantInfo()")[0] == "1";

        public ulong GetBackpackItemGuid(int slot) => MemoryManager.ReadUlong(GetDescriptorPtr() + (MemoryAddresses.LocalPlayer_BackpackFirstItemOffset + (slot * 8)));

        public ulong GetEquippedItemGuid(EquipSlot slot) => MemoryManager.ReadUlong(IntPtr.Add(Pointer, (MemoryAddresses.LocalPlayer_EquipmentFirstItemOffset + ((int)slot - 1) * 0x8)));
        
        public void CastSpell(string spellName, ulong targetGuid)
        {
            var spellId = GetSpellId(spellName);
            Functions.CastSpellById(spellId, targetGuid);
        }

        public bool InLosWith(Position position)
        {
            var i = Functions.Intersect(Position, position);
            return i.X == 0 && i.Y == 0 && i.Z == 0;
        }

        public bool CanRiposte
        {
            get
            {
                var results = LuaCallWithResults("{0} = IsUsableSpell(\"Riposte\")");
                if (results.Length > 0)
                    return results[0] == "1";
                else
                    return false;
            }
        }

        public void CastSpellAtPosition(string spellName, Position position)
        {
            return;
            // Functions.CastAtPosition(spellName, position);
        }

        public bool IsAutoRepeating(string name)
        {
            string luaString = $@"
                local i = 1
                while true do
                    local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL);
                    if not spellName then
                        break;
                    end
   
                    -- use spellName and spellRank here
                    if(spellName == ""{{{name}}}"") then
                        PickupSpell(i, BOOKTYPE_SPELL);
                        PlaceAction(1);
                        ClearCursor();
                        return IsAutoRepeatAction(1)
                    end

                    i = i + 1;
                end
                return false;";
            var result = LuaCallWithResults(luaString);
            Console.WriteLine(result);
            return false;
        }

        private static string FormatLua(string str, params object[] names)
        {
            return string.Format(str, names.Select(s => s.ToString().Replace("'", "\\'").Replace("\"", "\\\"")).ToArray());
        }

        // Keep track of current zone
        private static string m_CurrZone;
        public string CurrZone { get { return m_CurrZone; } set { m_CurrZone = value; } }

        // Keep track of current WP
        private static int m_CurrWpId;
        public int CurrWpId { get { return m_CurrWpId; } set { m_CurrWpId = value; } }

        // Keep track of last WP visited
        private static int m_LastWpId;
        public int LastWpId { get { return m_LastWpId; } set { m_LastWpId = value; } }

        // Keep track of deaths at WP
        private static int m_DeathsAtWp;
        public int DeathsAtWp { get { return m_DeathsAtWp; } set { m_DeathsAtWp = value; } }

        private static int m_WpStuckCount;
        public int WpStuckCount { get { return m_WpStuckCount; } set { m_WpStuckCount = value; } }

        private static bool m_HasOverleveled;
        public bool HasOverLeveled { get { return m_HasOverleveled; } set { m_HasOverleveled = value; } }

        private static List<int> m_ForcedWpPath;
        public List<int> ForcedWpPath { get { return m_ForcedWpPath; } set { m_ForcedWpPath = value; } }

        private static HashSet<int> m_VisitedWps;
        public bool HasVisitedWp(int id)
        {
            return m_VisitedWps.Contains(id);
        }
        public HashSet<int> VisitedWps { get { return m_VisitedWps; } set { m_VisitedWps = value; } }

        private static HashSet<int> m_BlackListedWps = new HashSet<int> {35, 118, 168, 993, 1093, 1094, 1100, 1180, 1359, 1364, 1369, 1426, 1438, 1444, 1445, 1456, 1462, 1463, 1464, 1465, 1466, 1566, 1614, 2388, 2571, 5057, 5069};
        public HashSet<int> BlackListedWps { get { return m_BlackListedWps; } set { m_BlackListedWps = value; } }

        private static bool m_HasBeenStuckAtWp;
        public bool HasBeenStuckAtWp { get { return m_HasBeenStuckAtWp; } set { m_HasBeenStuckAtWp = value; } }

        private static bool m_HasJoinedBg;
        public bool HasJoinedBg { get { return m_HasJoinedBg; } set { m_HasJoinedBg = value; } }

        private static bool m_HasEnteredNewMap;
        public bool HasEnteredNewMap { get { return m_HasEnteredNewMap; } set { m_HasEnteredNewMap = value; } }

        private static bool m_ShouldWaitForShortDelay;
        public bool ShouldWaitForShortDelay { get { return m_ShouldWaitForShortDelay; } set { m_ShouldWaitForShortDelay = value; } }
        
        private static bool m_HasItemsToEquip;
        public bool HasItemsToEquip { get { return m_HasItemsToEquip; } set { m_HasItemsToEquip = value; } }

        private static uint m_LastKnownMapId;
        public uint LastKnownMapId { get { return m_LastKnownMapId; } set { m_LastKnownMapId = value; } }

        private static HashSet<ulong> m_BlackListedTargets = new HashSet<ulong> {};
        public HashSet<ulong> BlackListedTargets { get { return m_BlackListedTargets; } set { m_BlackListedTargets = value; } }

        public List<string> FoodNames { get { return s_FoodNames; } set { s_FoodNames = value; } }
        private static List<string> s_FoodNames = new List<string> 
        {
            "Conjured Cinnamon Roll", "Conjured Croissant", "Conjured Pumpernickel", 
            "Conjured Rye", "Conjured Muffin", "Conjured Sourdough", 
            "Conjured Mana Pie", "Conjured Sweet Roll", "Conjured Bread", 
            "Conjured Mana Strudel", "Conjured Mana Biscuit"
        };

        public List<string> DrinkNames { get { return s_DrinkNames; } set { s_DrinkNames = value; } }
        private static List<string> s_DrinkNames = new List<string>
        {
            "Conjured Crystal Water", "Conjured Purified Water", "Conjured Fresh Water",
            "Conjured Spring Water", "Conjured Water", "Conjured Mineral Water",
            "Conjured Sparkling Water", "Conjured Glacier Water",
            "Conjured Mountain Spring Water", "Conjured Mana Strudel",
            "Conjured Mana Biscuit", "Conjured Mana Pie"
        };

        private static Dictionary<int, string> m_HotspotRepairDict = new Dictionary<int, string>
        {
            { 1, ".npcb wp go 31" },     // Kalimdor horde repair
            { 2, ".npcb wp go 34" },     // Kalimdor alliance repair
            { 3, ".npcb wp go 4" },      // EK horde repair
            { 4, ".npcb wp go 13" },     // EK alliance repair
            { 5, ".npcb wp go 2578" },   // Outland horde repair
            { 6, ".npcb wp go 2601" },   // Outland alliance repair
            { 7, ".npcb wp go 2730" },   // Northrend horde repair
            { 8, ".npcb wp go 2703" }    // Northrend alliance repair
        };
        public Dictionary<int, string> HotspotRepairDict => m_HotspotRepairDict;

        public Dictionary<int, List<int>> LevelItemsDict => m_LevelItemsDict;
        private static Dictionary<int, List<int>> m_LevelItemsDict = new Dictionary<int, List<int>>
        {
            {10, new List<int> { 6659, 3344 }}, {12, new List<int> { 6378 }}, {14, new List<int> { 2583 }}, 
            {15, new List<int> { 21934, 12977 }}, {16, new List<int> { 12984 }}, {17, new List<int> { 1974 }},
            {20, new List<int> { 21566, 38383 }}, {21, new List<int> { 6463 }}, {24, new List<int> { 7048 }},
            {28, new List<int> { 9448 }}, {29, new List<int> { 9395 }}, {30, new List<int> { 18586, 7691 }},
            {31, new List<int> { 29157, 4743 }}, {32, new List<int> { 13105, 7514 }}, {33, new List<int> { 10578, 2277 }},
            {34, new List<int> { 18427 }}, {37, new List<int> { 13064 }}, {40, new List<int> { 10019 }},
            {41, new List<int> { 9433 }}, {42, new List<int> { 13102 }}, {43, new List<int> { 6440 }},
            {44, new List<int> { 17755 }}, {45, new List<int> { 9484, 10629 }}, {52, new List<int> { 16703 }},
            {55, new List<int> { 13170, 23126 }}, {56, new List<int> { 13001 }}, {58, new List<int> { 18102, 19105, 22408 }},
            {59, new List<int> { 22339, 13141, 20697 }}, {60, new List<int> { 20716, 13965, 28040 }}, {64, new List<int> { 29315 }},
            {65, new List<int> { 27410 }}, {66, new List<int> { 27440 }}, {67, new List<int> { 30368, 29813 }},
            {68, new List<int> { 27948, 30932 }}, {69, new List<int> { 27784, 38257 }}, {70, new List<int> { 27746, 38250 }},
            {71, new List<int> { 27683, 27885 }}, {72, new List<int> { 35657, 44365 }}, {73, new List<int> { 40758, 35663 }},
            {74, new List<int> { 43160, 39649 }}, {77, new List<int> { 35679, 38613 }}, {78, new List<int> { 37038, 37113 }}
        };

        public Dictionary<int, List<int>> LevelSpellsDict => m_LevelSpellsDict;
        private static Dictionary<int, List<int>> m_LevelSpellsDict = new Dictionary<int, List<int>>
        {
            {4, new List<int> { 5504 }}, {6, new List<int> { 143, 587 }}, {10, new List<int> { 5505 }},
            {12, new List<int> { 145, 604, 597 }}, {14, new List<int> { 1460, 837, 2137 }},
            {20, new List<int> { 5506, 7322, 12051 }}, {22, new List<int> { 990, 2138, 6143 }},
            {24, new List<int> { 8450, 2139 }}, {26, new List<int> { 120, 865, 8406 }},
            {28, new List<int> { 1461 }}, {30, new List<int> { 6127, 7302, 8412 }},
            {31, new List<int> { 8401, 8457 }}, {32, new List<int> { 6129, 8407, 8461 }},
            {34, new List<int> { 6117, 8492 }}, {36, new List<int> { 8451, 8402 }},
            {38, new List<int> { 8413, 8408 }}, {40, new List<int> { 10138, 33389, 33392, 54753 }},
            {41, new List<int> { 7320, 6131, 8458 }}, {42, new List<int> { 8462, 10144, 10148, 10156, 10159 }},
            {44, new List<int> { 10179 }}, {46, new List<int> { 22782, 10197 }}, {48, new List<int> { 10173, 10149 }},
            {50, new List<int> { 10219, 10160, 10161, 10180 }}, {51, new List<int> { 10139 }},
            {54, new List<int> { 10150, 10199 }}, {56, new List<int> { 10157, 10181 }}, {58, new List<int> { 22783, 13033 }},
            {60, new List<int> { 10140 }}, {61, new List<int> { 27078 }}, {63, new List<int> { 27071 }},
            {64, new List<int> { 27134 }}, {65, new List<int> { 27087, 37420 }}, {66, new List<int> { 27070 }},
            {67, new List<int> { 33944, 27088 }}, {69, new List<int> { 27124, 27125, 27072 }},
            {70, new List<int> { 27079, 27090, 27126 }}, {71, new List<int> { 43023 }}, {74, new List<int> { 42832, 42872 }},
            {75, new List<int> { 42917, 43038 }}, {76, new List<int> { 43015 }}, {78, new List<int> { 42833, 43010, 42842 }},
            {79, new List<int> { 43008, 43024, 42931 }}, {80, new List<int> { 42995, 42873 }}
        };

        public Dictionary<int, string> LevelTalentsDict => m_LevelTalentsDict;
        private static Dictionary<int, string> m_LevelTalentsDict = new Dictionary<int, string>
        {
            {10, "3, 2"}, {11, "3, 2"}, {12, "3, 2"}, {13, "3, 2"}, {14, "3, 2"}, {15, "3, 4"},
            {16, "3, 4"}, {17, "3, 4"}, {18, "3, 6"}, {19, "3, 6"}, {20, "3, 6"}, {21, "3, 9"},
            {22, "3, 8"}, {23, "3, 8"}, {24, "3, 8"}, {25, "3, 13"}, {26, "3, 13"}, {27, "3, 13"},
            {28, "3, 12"}, {29, "3, 12"}, {30, "3, 12"}, {31, "3, 14"}, {32, "3, 5"}, {33, "3, 5"},
            {34, "3, 7"}, {35, "3, 7"}, {36, "3, 7"}, {37, "3, 17"}, {38, "3, 17"}, {39, "3, 16"},
            {40, "3, 20"}, {41, "3, 16"}, {42, "3, 16"}, {43, "3, 21"}, {44, "3, 21"}, {45, "3, 21"},
            {46, "3, 21"}, {47, "3, 21"}, {48, "3, 22"}, {49, "3, 22"}, {50, "3, 25"}, {51, "3, 26"},
            {52, "3, 26"}, {53, "3, 26"}, {54, "3, 23"}, {55, "3, 23"}, {56, "3, 27"}, {57, "3, 27"},
            {58, "3, 27"}, {59, "3, 27"}, {60, "3, 27"}, {61, "3, 18"}, {62, "3, 18"}, {63, "3, 18"},
            {64, "3, 19"}, {65, "3, 19"}, {66, "3, 28"}, {67, "3, 15"}, {68, "3, 15"}, {69, "3, 15"},
            {70, "2, 2"}, {71, "2, 2"}, {72, "2, 2"}, {73, "2, 1"}, {74, "2, 1"}, {75, "3, 3"},
            {76, "3, 3"}, {77, "3, 3"}, {78, "3, 1"}, {79, "3, 1"}, {80, "3, 1"}
        };
    }
}
