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

        private static HashSet<int> m_BlackListedWps = new HashSet<int> {35, 118, 168, 993, 1093, 1094, 1100, 1180, 1359, 1364, 1369, 1426, 1438, 1444, 1445, 1456, 1462, 1463, 1464, 1465, 1466, 1566, 1614, 2388, 2571, 5057};
        public HashSet<int> BlackListedWps { get { return m_BlackListedWps; } set { m_BlackListedWps = value; } }

        private static bool m_HasBeenStuckAtWp;
        public bool HasBeenStuckAtWp { get { return m_HasBeenStuckAtWp; } set { m_HasBeenStuckAtWp = value; } }

        private static bool m_HasJoinedBg;
        public bool HasJoinedBg { get { return m_HasJoinedBg; } set { m_HasJoinedBg = value; } }

        private static bool m_HasEnteredNewMap;
        public bool HasEnteredNewMap { get { return m_HasEnteredNewMap; } set { m_HasEnteredNewMap = value; } }

        private static uint m_LastKnownMapId;
        public uint LastKnownMapId { get { return m_LastKnownMapId; } set { m_LastKnownMapId = value; } }

        private static HashSet<ulong> m_BlackListedNeutralTargets = new HashSet<ulong> {};
        public HashSet<ulong> BlackListedNeutralTargets { get { return m_BlackListedNeutralTargets; } set { m_BlackListedNeutralTargets = value; } }

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

        private static Dictionary<int, string> m_LevelItemsDict = new Dictionary<int, string>
        {
            {10, "SendChatMessage('.additem 10047'); SendChatMessage('.additem 8350');"},
            {12, "SendChatMessage('.additem 14025');"},
            {14, "SendChatMessage('.additem 2583');"},
            {15, "SendChatMessage('.additem 21934'); SendChatMessage('.additem 12977');"},
            {16, "SendChatMessage('.additem 12984');"},
            {17, "SendChatMessage('.additem 1974');"},
            {20, "SendChatMessage('.additem 21566'); SendChatMessage('.additem 38383'); SendChatMessage('.additem 6463');"},
            {24, "SendChatMessage('.additem 7048');"},
            {28, "SendChatMessage('.additem 9448');"},
            {29, "SendChatMessage('.additem 9395');"},
            {30, "SendChatMessage('.additem 18586'); SendChatMessage('.additem 7691'); SendChatMessage('.additem 2277');"},
            {31, "SendChatMessage('.additem 29157'); SendChatMessage('.additem 4743');"},
            {32, "SendChatMessage('.additem 13105'); SendChatMessage('.additem 7514');"},
            {33, "SendChatMessage('.additem 10578'); SendChatMessage('.additem 18427');"},
            {37, "SendChatMessage('.additem 13064');"},
            {40, "SendChatMessage('.additem 10019');"},
            {41, "SendChatMessage('.additem 9433');"},
            {42, "SendChatMessage('.additem 13102');"},
            {43, "SendChatMessage('.additem 6440');"},
            {44, "SendChatMessage('.additem 17755');"},
            {45, "SendChatMessage('.additem 9484'); SendChatMessage('.additem 10629');"},
            {52, "SendChatMessage('.additem 16703');"},
            {55, "SendChatMessage('.additem 13170'); SendChatMessage('.additem 23126'); SendChatMessage('.additem 13001');"},
            {58, "SendChatMessage('.additem 18102'); SendChatMessage('.additem 19105'); SendChatMessage('.additem 22408');"},
            {59, "SendChatMessage('.additem 22339'); SendChatMessage('.additem 13141'); SendChatMessage('.additem 20697');"},
            {60, "SendChatMessage('.additem 20716'); SendChatMessage('.additem 13965'); SendChatMessage('.additem 28040');"}
        };
        public Dictionary<int, string> LevelItemsDict => m_LevelItemsDict;

        private static Dictionary<int, string> m_EquipLevelItemsDict = new Dictionary<int, string>
        {
            {10, "EquipItemByName(10047); EquipItemByName(8350);"},
            {12, "EquipItemByName(14025);"},
            {14, "EquipItemByName(2583);"},
            {15, "EquipItemByName(21934); EquipItemByName(12977);"},
            {16, "EquipItemByName(12984);"},
            {17, "EquipItemByName(1974);"},
            {20, "EquipItemByName(21566); EquipItemByName(38383); EquipItemByName(6463);"},
            {24, "EquipItemByName(7048);"},
            {28, "EquipItemByName(9448);"},
            {29, "EquipItemByName(9395);"},
            {30, "EquipItemByName(18586); EquipItemByName(7691); EquipItemByName(2277);"},
            {31, "EquipItemByName(29157); EquipItemByName(4743);"},
            {32, "EquipItemByName(13105); EquipItemByName(7514);"},
            {33, "EquipItemByName(10578); EquipItemByName(18427);"},
            {37, "EquipItemByName(13064);"},
            {40, "EquipItemByName(10019);"},
            {41, "EquipItemByName(9433);"},
            {42, "EquipItemByName(13102);"},
            {43, "EquipItemByName(6440);"},
            {44, "EquipItemByName(17755);"},
            {45, "EquipItemByName(9484); EquipItemByName(10629);"},
            {52, "EquipItemByName(16703);"},
            {55, "EquipItemByName(13170); EquipItemByName(23126); EquipItemByName(13001);"},
            {58, "EquipItemByName(18102); EquipItemByName(19105); EquipItemByName(22408);"},
            {59, "EquipItemByName(22339); EquipItemByName(13141); EquipItemByName(20697);"},
            {60, "EquipItemByName(20716); EquipItemByName(13965); EquipItemByName(28040);"}
        };
        public Dictionary<int, string> EquipLevelItemsDict => m_EquipLevelItemsDict;
    }
}
