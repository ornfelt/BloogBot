using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the game's local player.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a player character that is controlled by the local machine.
    /// </summary>
    /// <summary>
    /// Represents a player character that is controlled by the local machine.
    /// </summary>
    public class LocalPlayer : WoWPlayer
    {
        /// <summary>
        /// Initializes a new instance of the LocalPlayer class with the specified pointer, GUID, and object type.
        /// </summary>
        internal LocalPlayer(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
            RefreshSpells();
        }

        /// <summary>
        /// Represents a random number generator.
        /// </summary>
        readonly Random random = new Random();

        /// <summary>
        /// Represents a warrior with the battle stance.
        /// </summary>
        // WARRIOR
        const string BattleStance = "Battle Stance";
        /// <summary>
        /// Represents the constant string value for "Berserker Stance".
        /// </summary>
        const string BerserkerStance = "Berserker Stance";
        /// <summary>
        /// Represents the constant string value for "Defensive Stance".
        /// </summary>
        const string DefensiveStance = "Defensive Stance";

        /// <summary>
        /// Represents the constant string value for "Bear Form".
        /// </summary>
        // DRUID
        const string BearForm = "Bear Form";
        /// <summary>
        /// Represents the constant string "Cat Form".
        /// </summary>
        const string CatForm = "Cat Form";

        /// <summary>
        /// Opcode for setting the facing direction. To be confirmed.
        /// </summary>
        // OPCODES
        const int SET_FACING_OPCODE = 0xDA; // TBC

        /// <summary>
        /// Dictionary that stores the spells of each player.
        /// The key is the player's name and the value is an array of integers representing the spells.
        /// </summary>
        public readonly IDictionary<string, int[]> PlayerSpells = new Dictionary<string, int[]>();

        /// <summary>
        /// Gets or sets the target WoWUnit.
        /// </summary>
        public WoWUnit Target { get; set; }

        /// <summary>
        /// Represents a boolean value indicating whether the object is currently turning.
        /// </summary>
        bool turning;
        /// <summary>
        /// Represents the total number of turns.
        /// </summary>
        int totalTurns;
        /// <summary>
        /// Represents the number of turns taken.
        /// </summary>
        int turnCount;
        /// <summary>
        /// The amount of something per turn.
        /// </summary>
        float amountPerTurn;
        /// <summary>
        /// Represents the position of an object while it is turning towards a target.
        /// </summary>
        Position turningToward;

        /// <summary>
        /// Retrieves the class of the local player by reading a byte from the memory address of the local player's class.
        /// </summary>
        public Class Class => (Class)MemoryManager.ReadByte((IntPtr)MemoryAddresses.LocalPlayerClass);

        /// <summary>
        /// Writes the current tick count to the last hardware action memory address, effectively preventing the user from going AFK.
        /// </summary>
        public void AntiAfk() => MemoryManager.WriteInt((IntPtr)MemoryAddresses.LastHardwareAction, Environment.TickCount);

        /// <summary>
        /// Gets the position of the corpse of the local player.
        /// </summary>
        public Position CorpsePosition => new Position(
                    MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionX),
                    MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionY),
                    MemoryManager.ReadFloat((IntPtr)MemoryAddresses.LocalPlayerCorpsePositionZ));

        /// <summary>
        /// Faces the specified position.
        /// </summary>
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

        /// <summary>
        /// Determines if the given position is within the cleave radius, which is larger than the facing radius.
        /// </summary>
        // Nat added this to see if he could test out the cleave radius which is larger than that isFacing radius
        public bool IsInCleave(Position position) => Math.Abs(GetFacingForPosition(position) - Facing) < 3f;

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
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

        /// <summary>
        /// Moves the object towards the specified position.
        /// </summary>
        public void MoveToward(Position pos)
        {
            Face(pos);
            StartMovement(ControlBits.Front);
        }

        /// <summary>
        /// Resets the facing state by setting all relevant variables to their initial values and stopping any strafing movement.
        /// </summary>
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

        /// <summary>
        /// Turns the object 180 degrees.
        /// </summary>
        public void Turn180()
        {
            var newFacing = Facing + Math.PI;
            if (newFacing > (Math.PI * 2))
                newFacing -= Math.PI * 2;
            SetFacing((float)newFacing);
        }

        /// <summary>
        /// Starts the movement based on the given control bits.
        /// The client will NOT send a packet to the server if a key is already pressed, so you're safe to spam this.
        /// </summary>
        // the client will NOT send a packet to the server if a key is already pressed, so you're safe to spam this
        public void StartMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Logger.LogVerbose($"StartMovement: {bits}");

            Functions.SetControlBit((int)bits, 1, Environment.TickCount);
        }

        /// <summary>
        /// Stops all movement in the specified directions.
        /// </summary>
        public void StopAllMovement()
        {
            var bits = ControlBits.Front | ControlBits.Back | ControlBits.Left | ControlBits.Right | ControlBits.StrafeLeft | ControlBits.StrafeRight;

            StopMovement(bits);
        }

        /// <summary>
        /// Stops the movement based on the specified control bits.
        /// </summary>
        public void StopMovement(ControlBits bits)
        {
            if (bits == ControlBits.Nothing)
                return;

            Logger.LogVerbose($"StopMovement: {bits}");
            Functions.SetControlBit((int)bits, 0, Environment.TickCount);
        }

        /// <summary>
        /// Performs a jump action. If the client version is Vanilla, it stops the movement and then starts the jump movement. Otherwise, it calls the Jump function from the Functions class.
        /// </summary>
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

        /// <summary>
        /// Use this property to determine whether you can use cannibalize.
        /// </summary>
        // use this to determine whether you can use cannibalize
        public bool TastyCorpsesNearby =>
            ObjectManager.Units.Any(u =>
                u.Position.DistanceTo(Position) < 5
                && u.CreatureType.HasFlag(CreatureType.Humanoid | CreatureType.Undead)
            );

        /// <summary>
        /// Calls the Lua function "DoEmote" with the parameter "STAND" to make the character stand.
        /// </summary>
        public void Stand() => LuaCall("DoEmote(\"STAND\")");

        /// <summary>
        /// Gets the current stance based on the buffs.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the player is in ghost form.
        /// </summary>
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

        /// <summary>
        /// Sets the target using the specified GUID.
        /// </summary>
        public void SetTarget(ulong guid) => Functions.SetTarget(guid);

        /// <summary>
        /// Gets or sets the ComboPointGuid.
        /// </summary>
        ulong ComboPointGuid { get; set; }
        /// <summary>
        /// Determines if the player can overpower the target.
        /// </summary>
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

        /// <summary>
        /// Gets the number of combo points on the target.
        /// </summary>
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

        /// <summary>
        /// Gets the current shapeshift form.
        /// </summary>
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

        /// <summary>
        /// Checks if the player is diseased by checking for any debuffs of type Disease.
        /// </summary>
        public bool IsDiseased => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Disease);

        /// <summary>
        /// Checks if the player is cursed by examining the debuffs.
        /// </summary>
        public bool IsCursed => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Curse);

        /// <summary>
        /// Checks if the player is poisoned by checking for any debuffs of type "Poison".
        /// </summary>
        public bool IsPoisoned => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Poison);

        /// <summary>
        /// Checks if the player has any magic debuffs.
        /// </summary>
        public bool HasMagicDebuff => GetDebuffs(LuaTarget.Player).Any(t => t.Type == EffectType.Magic);

        /// <summary>
        /// Releases the corpse.
        /// </summary>
        public void ReleaseCorpse() => Functions.ReleaseCorpse(Pointer);

        /// <summary>
        /// Retrieves the corpse.
        /// </summary>
        public void RetrieveCorpse() => Functions.RetrieveCorpse();

        /// <summary>
        /// Refreshes the list of player spells.
        /// </summary>
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
                    var spellPtr = MemoryManager.ReadIntPtr(spellsBasePtr + currentSpellId * 4);

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

        /// <summary>
        /// Gets the spell ID for a given spell name and rank.
        /// </summary>
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

        /// <summary>
        /// Checks if a spell is ready to be cast.
        /// </summary>
        public bool IsSpellReady(string spellName, int rank = -1)
        {
            if (!PlayerSpells.ContainsKey(spellName))
                return false;

            var spellId = GetSpellId(spellName, rank);

            return !Functions.IsSpellOnCooldown(spellId);
        }

        /// <summary>
        /// Retrieves the mana cost of a spell based on its name and rank.
        /// </summary>
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

        /// <summary>
        /// Checks if the player knows a specific spell.
        /// </summary>
        public bool KnowsSpell(string name) => PlayerSpells.ContainsKey(name);

        /// <summary>
        /// Checks if the main hand weapon is enchanted.
        /// </summary>
        public bool MainhandIsEnchanted => LuaCallWithResults("{0} = GetWeaponEnchantInfo()")[0] == "1";

        /// <summary>
        /// Retrieves the unique identifier of an item in the backpack at the specified slot.
        /// </summary>
        public ulong GetBackpackItemGuid(int slot) => MemoryManager.ReadUlong(GetDescriptorPtr() + (MemoryAddresses.LocalPlayer_BackpackFirstItemOffset + (slot * 8)));

        /// <summary>
        /// Retrieves the GUID of the equipped item in the specified equipment slot.
        /// </summary>
        public ulong GetEquippedItemGuid(EquipSlot slot) => MemoryManager.ReadUlong(IntPtr.Add(Pointer, (MemoryAddresses.LocalPlayer_EquipmentFirstItemOffset + ((int)slot - 1) * 0x8)));

        /// <summary>
        /// Casts a spell by its name on a target with the specified GUID.
        /// </summary>
        public void CastSpell(string spellName, ulong targetGuid)
        {
            var spellId = GetSpellId(spellName);
            Functions.CastSpellById(spellId, targetGuid);
        }

        /// <summary>
        /// Determines if the current position is in line of sight with the specified position.
        /// </summary>
        public bool InLosWith(Position position)
        {
            var i = Functions.Intersect(Position, position);
            return i.X == 0 && i.Y == 0 && i.Z == 0;
        }

        /// <summary>
        /// Gets a value indicating whether the character can use the "Riposte" spell.
        /// </summary>
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

        /// <summary>
        /// Casts a spell at a specified position.
        /// </summary>
        public void CastSpellAtPosition(string spellName, Position position)
        {
            return;
            // Functions.CastAtPosition(spellName, position);
        }

        /// <summary>
        /// Checks if a spell with the specified name is set to auto-repeat.
        /// </summary>
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

        /// <summary>
        /// Formats a Lua string with the provided arguments.
        /// </summary>
        private static string FormatLua(string str, params object[] names)
        {
            return string.Format(str, names.Select(s => s.ToString().Replace("'", "\\'").Replace("\"", "\\\"")).ToArray());
        }

        /// <summary>
        /// Keeps track of the current zone.
        /// </summary>
        // Keep track of current zone
        private static string m_CurrZone;
        /// <summary>
        /// Gets or sets the current zone.
        /// </summary>
        public string CurrZone { get { return m_CurrZone; } set { m_CurrZone = value; } }

        /// <summary>
        /// Keeps track of the current WP.
        /// </summary>
        // Keep track of current WP
        private static int m_CurrWpId;
        /// <summary>
        /// Gets or sets the current waypoint ID.
        /// </summary>
        public int CurrWpId { get { return m_CurrWpId; } set { m_CurrWpId = value; } }

        /// <summary>
        /// Keeps track of the last WP visited.
        /// </summary>
        // Keep track of last WP visited
        private static int m_LastWpId;
        /// <summary>
        /// Gets or sets the last waypoint ID.
        /// </summary>
        public int LastWpId { get { return m_LastWpId; } set { m_LastWpId = value; } }

        /// <summary>
        /// Keeps track of deaths at WP.
        /// </summary>
        // Keep track of deaths at WP
        private static int m_DeathsAtWp;
        /// <summary>
        /// Gets or sets the number of deaths at the waypoint.
        /// </summary>
        public int DeathsAtWp { get { return m_DeathsAtWp; } set { m_DeathsAtWp = value; } }

        /// <summary>
        /// The number of times the work process has been stuck.
        /// </summary>
        private static int m_WpStuckCount;
        /// <summary>
        /// Gets or sets the number of times the waypoint has been stuck.
        /// </summary>
        public int WpStuckCount { get { return m_WpStuckCount; } set { m_WpStuckCount = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the object has overleveled.
        /// </summary>
        private static bool m_HasOverleveled;
        /// <summary>
        /// Gets or sets a value indicating whether the object has overleveled.
        /// </summary>
        public bool HasOverLeveled { get { return m_HasOverleveled; } set { m_HasOverleveled = value; } }

        /// <summary>
        /// The list of forced waypoint paths.
        /// </summary>
        private static List<int> m_ForcedWpPath;
        /// <summary>
        /// Gets or sets the forced waypoint path.
        /// </summary>
        public List<int> ForcedWpPath { get { return m_ForcedWpPath; } set { m_ForcedWpPath = value; } }

        /// <summary>
        /// The set of visited waypoints.
        /// </summary>
        private static HashSet<int> m_VisitedWps;
        /// <summary>
        /// Checks if the specified ID has been visited.
        /// </summary>
        public bool HasVisitedWp(int id)
        {
            return m_VisitedWps.Contains(id);
        }
        /// <summary>
        /// Gets or sets the visited waypoints.
        /// </summary>
        public HashSet<int> VisitedWps { get { return m_VisitedWps; } set { m_VisitedWps = value; } }

        /// <summary>
        /// Gets or sets the blacklisted waypoints.
        /// </summary>
        private static HashSet<int> m_BlackListedWps = new HashSet<int> { 
            35, 118, 168, 300, 320, 359, 993, 1093, 1094, 1100, 1180, 
            1359, 1364, 1369, 1426, 1438, 1444, 1445, 1456, 1462, 1463, 
            1464, 1465, 1466, 1566, 1614, 2388, 2571, 2584, 5057, 5060, 
            5068, 5069, 5070 };
        public HashSet<int> BlackListedWps { get { return m_BlackListedWps; } set { m_BlackListedWps = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the object has been stuck at a waypoint.
        /// </summary>
        private static bool m_HasBeenStuckAtWp;
        /// <summary>
        /// Gets or sets a value indicating whether the object has been stuck at a waypoint.
        /// </summary>
        public bool HasBeenStuckAtWp { get { return m_HasBeenStuckAtWp; } set { m_HasBeenStuckAtWp = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the user has joined the background.
        /// </summary>
        private static bool m_HasJoinedBg;
        /// <summary>
        /// Gets or sets a value indicating whether the user has joined the background.
        /// </summary>
        public bool HasJoinedBg { get { return m_HasJoinedBg; } set { m_HasJoinedBg = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the program has entered a new map.
        /// </summary>
        private static bool m_HasEnteredNewMap;
        /// <summary>
        /// Gets or sets a value indicating whether the player has entered a new map.
        /// </summary>
        public bool HasEnteredNewMap { get { return m_HasEnteredNewMap; } set { m_HasEnteredNewMap = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the program should wait for a short delay.
        /// </summary>
        private static bool m_ShouldWaitForShortDelay;
        /// <summary>
        /// Gets or sets a value indicating whether the program should wait for a short delay.
        /// </summary>
        public bool ShouldWaitForShortDelay { get { return m_ShouldWaitForShortDelay; } set { m_ShouldWaitForShortDelay = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the program should wait for the teleport delay.
        /// </summary>
        private static bool m_ShouldWaitForTeleportDelay;
        /// <summary>
        /// Gets or sets a value indicating whether the program should wait for teleport delay.
        /// </summary>
        public bool ShouldWaitForTeleportDelay { get { return m_ShouldWaitForTeleportDelay; } set { m_ShouldWaitForTeleportDelay = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether there are items to equip.
        /// </summary>
        private static bool m_HasItemsToEquip;
        /// <summary>
        /// Gets or sets a value indicating whether there are items to equip.
        /// </summary>
        public bool HasItemsToEquip { get { return m_HasItemsToEquip; } set { m_HasItemsToEquip = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the object should teleport to the last waypoint.
        /// </summary>
        private static bool m_ShouldTeleportToLastWp;
        /// <summary>
        /// Gets or sets a value indicating whether the object should teleport to the last waypoint.
        /// </summary>
        public bool ShouldTeleportToLastWp { get { return m_ShouldTeleportToLastWp; } set { m_ShouldTeleportToLastWp = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether the player is of faction Alliance or not.
        /// </summary>
        private static bool m_IsAlly;
        /// <summary>
        /// Gets or sets a value indicating whether the player is of faction Alliance or not.
        /// </summary>
        public bool IsAlly { get { return m_IsAlly; } set { m_IsAlly = value; } }

        /// <summary>
        /// The last known map ID.
        /// </summary>
        private static uint m_LastKnownMapId;
        /// <summary>
        /// Gets or sets the last known map ID.
        /// </summary>
        public uint LastKnownMapId { get { return m_LastKnownMapId; } set { m_LastKnownMapId = value; } }

        /// <summary>
        /// The count of times the program has been stuck in a state or position.
        /// </summary>
        private static uint m_StuckInStateOrPosCount;
        /// <summary>
        /// Gets or sets the count of times the object is stuck in a state or position.
        /// </summary>
        public uint StuckInStateOrPosCount { get { return m_StuckInStateOrPosCount; } set { m_StuckInStateOrPosCount = value; } }

        /// <summary>
        /// The set of blacklisted targets.
        /// </summary>
        private static HashSet<ulong> m_BlackListedTargets = new HashSet<ulong> { };
        /// <summary>
        /// Gets or sets the blacklisted targets.
        /// </summary>
        public HashSet<ulong> BlackListedTargets { get { return m_BlackListedTargets; } set { m_BlackListedTargets = value; } }

        /// <summary>
        /// Gets or sets the list of food names.
        /// </summary>
        public List<string> FoodNames { get { return s_FoodNames; } set { s_FoodNames = value; } }
        /// <summary>
        /// List of conjured food names.
        /// </summary>
        private static List<string> s_FoodNames = new List<string>
        {
            "Conjured Cinnamon Roll", "Conjured Croissant", "Conjured Pumpernickel",
            "Conjured Rye", "Conjured Muffin", "Conjured Sourdough",
            "Conjured Mana Pie", "Conjured Sweet Roll", "Conjured Bread",
            "Conjured Mana Strudel", "Conjured Mana Biscuit"
        };

        /// <summary>
        /// Gets or sets the list of drink names.
        /// </summary>
        public List<string> DrinkNames { get { return s_DrinkNames; } set { s_DrinkNames = value; } }
        /// <summary>
        /// List of drink names.
        /// </summary>
        private static List<string> s_DrinkNames = new List<string>
        {
            "Conjured Crystal Water", "Conjured Purified Water", "Conjured Fresh Water",
            "Conjured Spring Water", "Conjured Water", "Conjured Mineral Water",
            "Conjured Sparkling Water", "Conjured Glacier Water",
            "Conjured Mountain Spring Water", "Conjured Mana Strudel",
            "Conjured Mana Biscuit", "Conjured Mana Pie"
        };

        /// <summary>
        /// Dictionary containing the commands for repairing hotspots in different locations.
        /// Key represents the location and value represents the repair command.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the dictionary for hotspot repairs.
        /// </summary>
        public Dictionary<int, string> HotspotRepairDict => m_HotspotRepairDict;

        /// <summary>
        /// Gets or sets the dictionary of level talents.
        /// </summary>
        public Dictionary<int, string> LevelTalentsDict => m_LevelTalentsDict;
        /// <summary>
        /// Dictionary that maps level numbers to talent values.
        /// </summary>
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

        /// <summary>
        /// Retrieves a list of all spells within a specified level range.
        /// </summary>
        public static List<int> GetAllSpellsInLevelRange(int min, int max)
        {
            return m_LevelSpellsDict
                .Where(kv => (kv.Key >= min && kv.Key <= max))
                .SelectMany(kv => kv.Value)
                .ToList();
        }

        /// <summary>
        /// Gets or sets the dictionary that maps level to a list of spells.
        /// </summary>
        public Dictionary<int, List<int>> LevelSpellsDict => m_LevelSpellsDict;

        /// <summary>
        /// Dictionary that stores the spells available at each level for a Mage character.
        /// The key represents the level, and the value is a list of spell IDs.
        /// </summary>
        private static Dictionary<int, List<int>> m_LevelSpellsDict = new Dictionary<int, List<int>>
        {
            {3, new List<int> { 1459 }}, // Arcane Intellect R1
            {4, new List<int> { 116, 5504 }}, // Frostbolt R1, Conjure Water R1
            {6, new List<int> { 143, 2136, 587 }}, // Fireball R2, Fire Blast R1, Conjure Food R1
            {8, new List<int> { 205 }}, // Frostbolt R2
            {10, new List<int> { 122, 5505, 7300 }}, // Frost Nova R1, Conjure Water R2, Frost Armor R2
            {12, new List<int> { 145, 597, 604, }}, // Fireball R3, Conjure Food R2, Dampen Magic R1
            {14, new List<int> { 1460, 837, 2137 }}, // Arcane Intellect R2, Frostbolt R3, Fire Blast R2
            {18, new List<int> { 3140 }}, // Fireball R4
            {20, new List<int> { 543, 5506, 7301, 7322, 12051 }}, // Fire ward R1, Conjure Water R3, Frost Armor R3, Frostbolt R4, Evocation
            {22, new List<int> { 990, 2138, 6143 }}, // Conjure Food R3, Fire Blast R3, Frost Ward R1
            {24, new List<int> { 8450, 2139, 8400 }}, // Dampen Magic R2, Counterspell, Fireball R5
            {26, new List<int> { 120, 865, 8406 }}, // Cone of Cold R1, Frost Nova R2, Frostbolt R5
            {28, new List<int> { 1461 }}, // Arcane Intellect R3
            {30, new List<int> { 6127, 7302, 8412 }}, // Conjure Water R4, Ice Armor R1, Fire Blast R4
            {31, new List<int> { 8401, 8457 }}, // Fireball R6, Fire Ward R2
            {32, new List<int> { 6129, 8407, 8461 }}, // Conjure Food R4, Frostbolt R6, Frost Ward R2
            {34, new List<int> { 6117, 8492 }}, // Mage Armor R1, Cone of Cold R2
            {36, new List<int> { 8451, 8402 }}, // Dampen Magic R3, Fireball R7
            {38, new List<int> { 8413, 8408 }}, // Fire Blast R5, Frostbolt R7
            {40, new List<int> { 10138, 33389, 33392, 54753 }}, // Conjure Water R5, Apprentice Riding, Journeyman Riding, White Polar Bear
            {41, new List<int> { 7320, 6131, 8458 }}, // Ice Armor R2, Frost Nova R3, Fire Ward R3
            {42, new List<int> { 8462, 10144, 10148, 10156, 10159 }}, // Frost Ward R3, Conjure Food R5, Fireball R8, Arcane Intellect R4, Cone of Cold R3
            {44, new List<int> { 10179 }}, // Frostbolt R8
            {46, new List<int> { 13031, 22782, 10197 }}, // Ice Barrier R2, Mage Armor R2, Fire Blast R6
            {48, new List<int> { 10173, 10149 }}, // Dampen Magic R4, Fireball R9
            {50, new List<int> { 10219, 10160, 10161, 10180 }}, // Ice Armor R3, Cone of Cold R4, Cone of Cold R5, Frostbolt R9
            {51, new List<int> { 10139 }}, // Conjure Water R6
            {52, new List<int> { 10145, 13032 }}, // Conjure Food R6, Ice Barrier R3
            {54, new List<int> { 10230, 10150, 10199 }}, // Frost Nova R4, Fireball R10, Fire Blast R7
            {56, new List<int> { 10157, 10181 }}, // Arcane Intellect R5, Frostbolt R10
            {58, new List<int> { 22783, 13033 }}, // Mage Armor R3, Ice Barrier R4
            {60, new List<int> { 10140, 10151, 25304, 25306, 28612 }}, // Conjure Water R7, Fireball R11, Frostbolt R11, Fireball R12, Conjure Food R7
            {61, new List<int> { 27078 }}, // Fire Blast R8
            {63, new List<int> { 27071 }}, // Frostbolt R12
            {64, new List<int> { 27134 }}, // Ice Barrier R5
            {65, new List<int> { 27087, 37420 }}, // Cone of Cold R6, Conjure Water R8
            {66, new List<int> { 27070 }}, // Fireball R13
            {67, new List<int> { 33944, 27088 }}, // Dampen Magic R6, Frost Nova R5
            {69, new List<int> { 27124, 27125, 27072 }}, // Ice Armor R5, Mage Armor R4, Frostbolt R13
            {70, new List<int> { 27079, 27090, 27126, 33717, 38692, 38697 }}, // Fire Blast R9, Conjure Water R9, Arcane Intellect R6, Conjure Food R8, Fireball R14, Frostbolt R14
            {71, new List<int> { 33405, 43023 }}, // Ice Barrier R6, Mage Armor R5
            {74, new List<int> { 42832, 42872 }}, // Fireball R15, Fire Blast R10
            {75, new List<int> { 42841, 42917, 43038 }}, // Frostbolt R15, Frost Nova R6, Ice Barrier R7
            {76, new List<int> { 43015 }}, // Dampen Magic R7
            {78, new List<int> { 42833, 43010, 42842 }}, // Fireball R16, Fire Ward R7, Frostbolt R16
            {79, new List<int> { 42842, 43008, 43024, 42931 }}, // Frostbolt R17, Ice Armor R6, Mage Armor R6, Cone of Cold R8
            {80, new List<int> { 42995, 42873, 43039 }} // Arcane Intellect R7, Fire Blast R11, Ice Barrier R8
        };

        /// <summary>
        /// Gets or sets the dictionary that maps level IDs to lists of item IDs.
        /// </summary>
        public Dictionary<int, List<int>> LevelItemsDict => m_LevelItemsDict;
        /// <summary>
        /// Dictionary that maps a level to a list of item IDs.
        /// </summary>
        private static Dictionary<int, List<int>> m_LevelItemsDict = new Dictionary<int, List<int>>
        {
            {10, new List<int> { 6659, 3344 }}, // Legs, Waist
            {12, new List<int> { 6378 }}, // Back
            {14, new List<int> { 2583 }}, // Feet
            {15, new List<int> { 21934, 12977 }}, // Neck, Hands
            {16, new List<int> { 12984 }}, // Wand
            {17, new List<int> { 1974 }}, // Wrist
            {20, new List<int> { 21566, 38383 }}, // Trinket, Trinket
            {21, new List<int> { 6463 }}, // Finger
            {24, new List<int> { 7048 }}, // Head
            {28, new List<int> { 9448 }}, // Wrist
            {29, new List<int> { 9395 }}, // Hands
            {30, new List<int> { 18586, 7691 }}, // Finger, Head
            {31, new List<int> { 29157, 4743 }}, // Finger, Neck
            {32, new List<int> { 13105, 7514 }}, // Waist, Wand
            {33, new List<int> { 10578, 2277 }}, // Feet, Legs
            {34, new List<int> { 18427 }}, // Back
            {37, new List<int> { 13064 }}, // Wand
            {40, new List<int> { 10019 }}, // Hands
            {41, new List<int> { 9433 }}, // Wrist
            {42, new List<int> { 13102 }}, // Head
            {43, new List<int> { 6440 }}, // Finger
            {44, new List<int> { 17755 }}, // Waist
            {45, new List<int> { 9484, 10629 }}, // Legs, Feet
            {52, new List<int> { 16703 }}, // Wrist
            {55, new List<int> { 13170, 23126 }}, // Legs, Waist
            {56, new List<int> { 13001 }}, // Finger
            {58, new List<int> { 18102, 19105, 22408 }}, // Feet, Head, Wand
            {59, new List<int> { 22339, 13141, 20697 }}, // Finger, Neck, Back
            {61, new List<int> { 20716, 13965, 28040 }}, // Hands, Trinket, Trinket
            {64, new List<int> { 29315 }}, // Hands
            {65, new List<int> { 27410 }}, // Head
            {66, new List<int> { 27440 }}, // Neck
            {67, new List<int> { 30368, 29813 }}, // Feet, Back
            {68, new List<int> { 27948, 30932 }}, // Legs, Waist
            {69, new List<int> { 27784, 38257 }}, // Finger, Trinket
            {71, new List<int> { 27462, 27683, 27885 }}, // Wrist, Trinket, Wand
            {72, new List<int> { 35657, 38250, 44365 }}, // Feet, Finger, Hands
            {73, new List<int> { 40758, 35663 }}, // Head, Waist
            {74, new List<int> { 43160, 39649 }}, // Legs, Finger
            {77, new List<int> { 35679, 38613 }}, // Head, Neck
            {78, new List<int> { 37038, 37113 }} // Wand, Wrist
        };

        /// <summary>
        /// The name of the bot's npcbot friend.
        /// </summary>
        private static string m_BotFriend; // Lazarus (horde) or Zalduun (ally)
        /// <summary>
        /// The name of the bot's npcbot friend.
        /// </summary>
        public string BotFriend { get { return m_BotFriend; } set { m_BotFriend = value; } }
    }
}
