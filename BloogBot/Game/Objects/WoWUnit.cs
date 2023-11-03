using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a unit in the World of Warcraft game.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a unit in the World of Warcraft game.
    /// </summary>
    /// <summary>
    /// Represents a unit in the World of Warcraft game.
    /// </summary>
    public class WoWUnit : WoWObject
    {
        /// <summary>
        /// Array of strings representing immobilized spell text.
        /// </summary>
        static readonly string[] ImmobilizedSpellText = { "Immobilized" };

        /// <summary>
        /// Initializes a new instance of the <see cref="WoWUnit"/> class.
        /// </summary>
        public WoWUnit() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WoWUnit"/> class.
        /// </summary>
        public WoWUnit(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
        }

        /// <summary>
        /// Gets the target GUID of the WoWUnit.
        /// </summary>
        public ulong TargetGuid => MemoryManager.ReadUlong(GetDescriptorPtr() + MemoryAddresses.WoWUnit_TargetGuidOffset);

        /// <summary>
        /// Gets the health of the WoWUnit by reading the integer value from the memory address offset.
        /// </summary>
        public int Health => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_HealthOffset);
        /// <summary>
        /// Gets the maximum health of the unit.
        /// </summary>
        //public int Health
        //{
        //    get
        //    {
        //        Console.WriteLine("Fetching health... GetDescPtr: " + GetDescriptorPtr());
        //        Console.WriteLine("healthOffset: " + MemoryAddresses.WoWUnit_HealthOffset);
        //        int health = MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_HealthOffset);
        //        Console.WriteLine("Health: " + health);
        //        return health;
        //    }
        //}

        public int MaxHealth => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_MaxHealthOffset);

        /// <summary>
        /// Calculates the health percentage based on the current health and maximum health.
        /// </summary>
        public int HealthPercent => (int)(Health / (float)MaxHealth * 100);

        /// <summary>
        /// Gets the current mana of the WoWUnit.
        /// </summary>
        public int Mana => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_ManaOffset);

        /// <summary>
        /// Gets the maximum mana of the WoWUnit.
        /// </summary>
        public int MaxMana => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_MaxManaOffset);

        /// <summary>
        /// Calculates the percentage of mana remaining.
        /// </summary>
        public int ManaPercent => (int)(Mana / (float)MaxMana * 100);

        /// <summary>
        /// Gets the rage value of the WoWUnit by reading the integer value from the memory address offset and dividing it by 10.
        /// </summary>
        public int Rage => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_RageOffset) / 10;

        /// <summary>
        /// Gets the energy value of the WoWUnit.
        /// </summary>
        public int Energy => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_EnergyOffset);

        /// <summary>
        /// Gets the current channeling ID.
        /// </summary>
        public int CurrentChannelingId
        {
            get
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    return MemoryManager.ReadInt(GetDescriptorPtr() + 0x240);
                }
                else
                {
                    return MemoryManager.ReadInt(Pointer + MemoryAddresses.WoWUnit_CurrentChannelingOffset);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current channeling ID is greater than 0.
        /// </summary>
        public bool IsChanneling => CurrentChannelingId > 0;

        /// <summary>
        /// Gets the current spellcast ID of the WoWUnit.
        /// </summary>
        public int CurrentSpellcastId => MemoryManager.ReadInt(Pointer + MemoryAddresses.WoWUnit_CurrentSpellcastOffset);

        /// <summary>
        /// Gets a value indicating whether the current spellcast ID is greater than 0.
        /// </summary>
        public bool IsCasting => CurrentSpellcastId > 0;

        /// <summary>
        /// Gets the level of the WoWUnit.
        /// </summary>
        public virtual int Level => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_LevelOffset);

        /// <summary>
        /// Gets the dynamic flags of the WoWUnit.
        /// </summary>
        public DynamicFlags DynamicFlags => (DynamicFlags)MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_DynamicFlagsOffset);

        /// <summary>
        /// Determines if the object can be looted based on its health and dynamic flags.
        /// </summary>
        public bool CanBeLooted => Health == 0 && DynamicFlags.HasFlag(DynamicFlags.CanBeLooted);

        /// <summary>
        /// Checks if the object has been tapped by another entity.
        /// </summary>
        public bool TappedByOther => DynamicFlags.HasFlag(DynamicFlags.Tapped) && !DynamicFlags.HasFlag(DynamicFlags.TappedByMe);

        /// <summary>
        /// Gets the unit flags by reading the memory at the descriptor pointer offsetted by the WoWUnit_UnitFlagsOffset.
        /// </summary>
        public UnitFlags UnitFlags => (UnitFlags)MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_UnitFlagsOffset);

        /// <summary>
        /// Checks if the unit is currently in combat.
        /// </summary>
        public bool IsInCombat => UnitFlags.HasFlag(UnitFlags.UNIT_FLAG_IN_COMBAT);

        /// <summary>
        /// Checks if the unit is stunned based on the UnitFlags.
        /// </summary>
        public bool IsStunned => UnitFlags.HasFlag(UnitFlags.UNIT_FLAG_STUNNED);

        /// <summary>
        /// Gets the GUID of the unit that summoned this unit.
        /// </summary>
        public ulong SummonedByGuid => MemoryManager.ReadUlong(GetDescriptorPtr() + MemoryAddresses.WoWUnit_SummonedByGuidOffset);

        /// <summary>
        /// Gets the faction ID of the WoWUnit.
        /// </summary>
        public int FactionId => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_FactionIdOffset);

        /// <summary>
        /// Gets a value indicating whether the unit is attackable or not.
        /// </summary>
        public bool NotAttackable => UnitFlags.HasFlag(UnitFlags.UNIT_FLAG_NON_ATTACKABLE);

        /// <summary>
        /// Determines if the current object is facing a given position.
        /// </summary>
        public bool IsFacing(Position position) => Math.Abs(GetFacingForPosition(position) - Facing) < 0.05f;

        /// <summary>
        /// Calculates the facing angle in radians for a given position.
        /// </summary>
        // in radians
        public float GetFacingForPosition(Position position)
        {
            var f = (float)Math.Atan2(position.Y - Position.Y, position.X - Position.X);
            if (f < 0.0f)
                f += (float)Math.PI * 2.0f;
            else
            {
                if (f > (float)Math.PI * 2)
                    f -= (float)Math.PI * 2.0f;
            }
            return f;
        }

        /// <summary>
        /// Determines if the current object is behind the specified target.
        /// </summary>
        public bool IsBehind(WoWUnit target)
        {
            var halfPi = Math.PI / 2;
            var twoPi = Math.PI * 2;
            var leftThreshold = target.Facing - halfPi;
            var rightThreshold = target.Facing + halfPi;

            bool condition;
            if (leftThreshold < 0)
                condition = Facing < rightThreshold || Facing > twoPi + leftThreshold;
            else if (rightThreshold > twoPi)
                condition = Facing > leftThreshold || Facing < rightThreshold - twoPi;
            else
                condition = Facing > leftThreshold && Facing < rightThreshold;

            return condition && IsFacing(target.Position);
        }

        /// <summary>
        /// Gets the movement flags of the WoWUnit.
        /// </summary>
        public MovementFlags MovementFlags => (MovementFlags)MemoryManager.ReadInt(IntPtr.Add(Pointer, MemoryAddresses.WoWUnit_MovementFlagsOffset));

        /// <summary>
        /// Determines if the object is currently moving forward.
        /// </summary>
        public bool IsMoving => MovementFlags.HasFlag(MovementFlags.MOVEFLAG_FORWARD);

        /// <summary>
        /// Determines if the character is swimming based on the movement flags.
        /// </summary>
        public bool IsSwimming => MovementFlags.HasFlag(MovementFlags.MOVEFLAG_SWIMMING);

        /// <summary>
        /// Checks if the character is currently falling.
        /// </summary>
        public bool IsFalling => MovementFlags.HasFlag(MovementFlags.MOVEFLAG_FALLING);

        /// <summary>
        /// Gets a value indicating whether the unit is mounted.
        /// </summary>
        public bool IsMounted => UnitFlags.HasFlag(UnitFlags.UNIT_FLAG_MOUNT);

        /// <summary>
        /// Gets a value indicating whether this instance is a pet.
        /// </summary>
        public bool IsPet => SummonedByGuid > 0;

        /// <summary>
        /// Gets the creature type using the specified pointer.
        /// </summary>
        public CreatureType CreatureType => Functions.GetCreatureType(Pointer);

        /// <summary>
        /// Gets the unit reaction.
        /// </summary>
        public UnitReaction UnitReaction => Functions.GetUnitReaction(Pointer, ObjectManager.Player.Pointer);

        /// <summary>
        /// Gets the rank of the creature.
        /// </summary>
        public virtual CreatureRank CreatureRank => (CreatureRank)Functions.GetCreatureRank(Pointer);

        /// <summary>
        /// Retrieves a spell by its ID.
        /// </summary>
        public Spell GetSpellById(int spellId)
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                var spellsBasePtr = MemoryManager.ReadIntPtr((IntPtr)0x00C0D788);
                var spellPtr = MemoryManager.ReadIntPtr(spellsBasePtr + spellId * 4);

                var spellCost = MemoryManager.ReadInt(spellPtr + 0x0080);

                var spellNamePtr = MemoryManager.ReadIntPtr(spellPtr + 0x1E0);
                var spellName = MemoryManager.ReadString(spellNamePtr);

                var spellDescriptionPtr = MemoryManager.ReadIntPtr(spellPtr + 0x228);
                var spellDescription = MemoryManager.ReadString(spellDescriptionPtr);

                var spellTooltipPtr = MemoryManager.ReadIntPtr(spellPtr + 0x24C);
                var spellTooltip = MemoryManager.ReadString(spellTooltipPtr);

                return new Spell(spellId, spellCost, spellName, spellDescription, spellTooltip);
            }
            else
            {
                return Functions.GetSpellDBEntry(spellId);
            }
        }

        /// <summary>
        /// Calls a Lua function with the specified code.
        /// </summary>
        public void LuaCall(string code) => Functions.LuaCall(code);

        /// <summary>
        /// Calls a Lua function with the specified code and returns the results as an array of strings.
        /// </summary>
        public string[] LuaCallWithResults(string code) => Functions.LuaCallWithResult(code);

        /// <summary>
        /// Retrieves a collection of buffs for the player character.
        /// </summary>
        public IEnumerable<Spell> Buffs
        {
            get
            {
                // TODO: figure out what's going on here. WotLK seems to store buffs at a static offset from the Player Pointer,
                // but TBC seems to store them as a Descriptor
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    var count = Functions.GetAuraCount(Pointer);
                    var buffs = new List<Spell>();
                    for (var i = 0; i < count; i++)
                    {
                        var buffPtr = Functions.GetAuraPointer(Pointer, i);

                        var spellId = MemoryManager.ReadInt(buffPtr + 0x8);
                        if (spellId > 0) // some weird invisible auras exist?
                        {
                            var flags = (AuraFlags)MemoryManager.ReadInt(buffPtr + 0x10);
                            if (!flags.HasFlag(AuraFlags.Harmful))
                            {
                                buffs.Add(GetSpellById(spellId));
                            }
                        }

                    }
                    return buffs;
                }
                else
                {
                    var buffs = new List<Spell>();
                    var currentBuffOffset = MemoryAddresses.WoWUnit_BuffsBaseOffset;
                    for (var i = 0; i < 10; i++)
                    {
                        var buffId = MemoryManager.ReadInt(GetDescriptorPtr() + currentBuffOffset);
                        if (buffId != 0)
                            buffs.Add(GetSpellById(buffId));
                        currentBuffOffset += 4;
                    }
                    return buffs;
                }
            }
        }

        /// <summary>
        /// Retrieves a collection of debuffs on the player character.
        /// </summary>
        public IEnumerable<Spell> Debuffs
        {
            get
            {
                // TODO: figure out what's going on here. WotLK seems to store buffs at a static offset from the Player Pointer,
                // but TBC seems to store them as a Descriptor
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    var count = Functions.GetAuraCount(Pointer);
                    var buffs = new List<Spell>();
                    for (var i = 0; i < count; i++)
                    {
                        var buffPtr = Functions.GetAuraPointer(Pointer, i);

                        var spellId = MemoryManager.ReadInt(buffPtr + 0x8);
                        if (spellId > 0) // some weird invisible auras exist?
                        {
                            var flags = (AuraFlags)MemoryManager.ReadInt(buffPtr + 0x10);
                            if (flags.HasFlag(AuraFlags.Harmful))
                            {
                                buffs.Add(GetSpellById(spellId));
                            }
                        }
                    }
                    return buffs;
                }
                else if (ClientHelper.ClientVersion == ClientVersion.TBC)
                {
                    var debuffs = new List<Spell>();
                    var currentDebuffOffset = MemoryAddresses.WoWUnit_DebuffsBaseOffset;
                    for (var i = 0; i < 16; i++)
                    {
                        var debuffId = MemoryManager.ReadInt(Pointer + currentDebuffOffset);
                        if (debuffId != 0)
                            debuffs.Add(GetSpellById(debuffId));
                        currentDebuffOffset += 4;
                    }
                    return debuffs;
                }
                else
                {
                    var debuffs = new List<Spell>();
                    var currentDebuffOffset = MemoryAddresses.WoWUnit_DebuffsBaseOffset;
                    for (var i = 0; i < 16; i++)
                    {
                        var debuffId = MemoryManager.ReadInt(GetDescriptorPtr() + currentDebuffOffset);
                        if (debuffId != 0)
                            debuffs.Add(GetSpellById(debuffId));
                        currentDebuffOffset += 4;
                    }
                    return debuffs;
                }
            }
        }

        /// <summary>
        /// Retrieves a collection of debuffs for the specified LuaTarget.
        /// </summary>
        public IEnumerable<SpellEffect> GetDebuffs(LuaTarget target)
        {
            var debuffs = new List<SpellEffect>();

            for (var i = 1; i <= 16; i++)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var result = LuaCallWithResults("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10} = UnitDebuff('" + target.ToString().ToLower() + "', " + i + ")");
                    var icon = result[0];
                    var stackCount = result[1];
                    var debuffTypeString = result[2];

                    if (string.IsNullOrEmpty(icon))
                        break;

                    var success = Enum.TryParse(debuffTypeString, out EffectType type);
                    if (!success)
                        type = EffectType.None;

                    debuffs.Add(new SpellEffect(icon, Convert.ToInt32(stackCount), type));
                }
                else
                {
                    var result = LuaCallWithResults("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10} = UnitDebuff('" + target.ToString().ToLower() + "', " + i + ")");
                    var icon = result[2];
                    var stackCount = result[3];
                    var debuffTypeString = result[4];

                    if (string.IsNullOrEmpty(icon))
                        break;

                    var success = Enum.TryParse(debuffTypeString, out EffectType type);
                    if (!success)
                        type = EffectType.None;

                    debuffs.Add(new SpellEffect(icon, Convert.ToInt32(stackCount), type));
                }
            }

            return debuffs;
        }

        /// <summary>
        /// Checks if the specified buff exists.
        /// </summary>
        public bool HasBuff(string name) => Buffs.Any(a => a.Name == name);

        /// <summary>
        /// Checks if the specified debuff exists.
        /// </summary>
        public bool HasDebuff(string name) => Debuffs.Any(a => a.Name == name);

        /// <summary>
        /// Gets a value indicating whether the object is immobilized.
        /// </summary>
        public bool IsImmobilized
        {
            get
            {
                return Debuffs.Any(d => ImmobilizedSpellText.Any(s => d.Description.Contains(s) || d.Tooltip.Contains(s)));
            }
        }
    }
}
