using BloogBot.Game.Enums;
using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains classes for handling game functions specific to The Burning Crusade expansion.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a class that handles game functions for the TBC game.
    /// </summary>
    /// <summary>
    /// Represents a class that handles game functions for the TBC game.
    /// </summary>
    public class TBCGameFunctionHandler : IGameFunctionHandler
    {
        /// <summary>
        /// Represents a delegate for buying an item from a vendor.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void BuyVendorItemDelegate(ulong vendorGuid, int itemId, int quantity, int unused);

        /// <summary>
        /// Gets the delegate for the function pointer to buy a vendor item.
        /// </summary>
        static readonly BuyVendorItemDelegate BuyVendorItemFunction =
                    Marshal.GetDelegateForFunctionPointer<BuyVendorItemDelegate>((IntPtr)MemoryAddresses.BuyVendorItemFunPtr);

        /// <summary>
        /// Buys a specified quantity of an item from a vendor.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "BuyVendorItem" as B
        /// participant "BuyVendorItemFunction" as F
        /// B -> F: BuyVendorItemFunction(vendorGuid, itemId, quantity, 1)
        /// \enduml
        /// </remarks>
        public void BuyVendorItem(ulong vendorGuid, int itemId, int quantity)
        {
            BuyVendorItemFunction(vendorGuid, itemId, quantity, 1);
        }

        /// <summary>
        /// Represents a delegate used to cast at a specific position.
        /// </summary>
        /// <param name="parPos">The position to cast at.</param>
        /// <returns>The result of the cast operation.</returns>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int CastAtPositionDelegate(ref XYZ parPos);

        /// <summary>
        /// Retrieves the function pointer for the CastAtPositionDelegate and assigns it to the CastAtPositionFunction.
        /// </summary>
        static readonly CastAtPositionDelegate CastAtPositionFunction =
                    Marshal.GetDelegateForFunctionPointer<CastAtPositionDelegate>((IntPtr)MemoryAddresses.CastAtPositionFunPtr);

        /// <summary>
        /// Casts a spell at the specified position.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CastAtPosition -> MemoryManager: WriteByte((IntPtr)0xCECAC0, 0)
        /// CastAtPosition --> LuaCall: CastSpellByName(spellName)
        /// CastAtPosition -> position: ToXYZ()
        /// CastAtPosition --> CastAtPositionFunction: ref pos
        /// \enduml
        /// </remarks>
        public void CastAtPosition(string spellName, Position position)
        {
            MemoryManager.WriteByte((IntPtr)0xCECAC0, 0);
            LuaCall($"CastSpellByName('{spellName}')");
            var pos = position.ToXYZ();
            CastAtPositionFunction(ref pos);
        }

        /// <summary>
        /// Represents a delegate for casting a spell by its ID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int CastSpellByIdDelegate(int spellId, int unknown, ulong targetGuid);

        /// <summary>
        /// Retrieves the function pointer for the CastSpellByIdDelegate and assigns it to the CastSpellByIdFunction.
        /// </summary>
        static readonly CastSpellByIdDelegate CastSpellByIdFunction =
                    Marshal.GetDelegateForFunctionPointer<CastSpellByIdDelegate>((IntPtr)MemoryAddresses.CastSpellByIdFunPtr);

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "CastSpellById" as CS
        /// participant "CastSpellByIdFunction" as CSF
        /// C -> CS: CastSpellById(spellId, targetGuid)
        /// CS -> CSF: CastSpellByIdFunction(spellId, 0, targetGuid)
        /// CSF --> CS: return
        /// CS --> C: return
        /// \enduml
        /// </remarks>
        public int CastSpellById(int spellId, ulong targetGuid)
        {
            return CastSpellByIdFunction(spellId, 0, targetGuid);
        }

        /// <summary>
        /// Represents a delegate for dismounting a unit.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int DismountDelegate(IntPtr unitPtr);

        /// <summary>
        /// Gets the delegate for the dismount function pointer.
        /// </summary>
        static readonly DismountDelegate DismountFunction =
                    Marshal.GetDelegateForFunctionPointer<DismountDelegate>((IntPtr)MemoryAddresses.DismountFunPtr);

        /// <summary>
        /// Dismounts the unit specified by the given pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor Client
        /// Client -> Dismount: unitPtr
        /// Dismount -> DismountFunction: unitPtr
        /// DismountFunction --> Dismount: int
        /// Dismount --> Client: int
        /// \enduml
        /// </remarks>
        public int Dismount(IntPtr unitPtr)
        {
            return DismountFunction(unitPtr);
        }

        /// <summary>
        /// Represents a delegate for enumerating visible objects.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate char EnumerateVisibleObjectsDelegate(IntPtr callback, int filter);

        /// <summary>
        /// Retrieves the delegate for enumerating visible objects from the specified function pointer.
        /// </summary>
        static readonly EnumerateVisibleObjectsDelegate EnumerateVisibleObjectsFunction =
                    Marshal.GetDelegateForFunctionPointer<EnumerateVisibleObjectsDelegate>((IntPtr)MemoryAddresses.EnumerateVisibleObjectsFunPtr);

        /// <summary>
        /// Enumerates the visible objects using the specified callback and filter.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> EnumerateVisibleObjects : callback, filter
        /// EnumerateVisibleObjects -> EnumerateVisibleObjectsFunction : callback, filter
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        public void EnumerateVisibleObjects(IntPtr callback, int filter)
        {
            EnumerateVisibleObjectsFunction(callback, filter);
        }

        /// <summary>
        /// Represents a delegate used to get the rank of a creature.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int GetCreatureRankDelegate
                    (IntPtr unitPtr);

        /// <summary>
        /// Gets the rank of a creature.
        /// </summary>
        static readonly GetCreatureRankDelegate GetCreatureRankFunction =
                    Marshal.GetDelegateForFunctionPointer<GetCreatureRankDelegate>((IntPtr)MemoryAddresses.GetCreatureRankFunPtr);

        /// <summary>
        /// Retrieves the rank of a creature based on its unit pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor Client
        /// participant "GetCreatureRank()" as A
        /// participant "GetCreatureRankFunction()" as B
        ///
        /// Client -> A: unitPtr
        /// A -> B: unitPtr
        /// B --> A: rank
        /// A --> Client: rank
        /// \enduml
        /// </remarks>
        public int GetCreatureRank(IntPtr unitPtr)
        {
            return GetCreatureRankFunction(unitPtr);
        }

        /// <summary>
        /// Represents a delegate used to get the creature type of a unit.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int GetCreatureTypeDelegate(IntPtr unitPtr);

        /// <summary>
        /// Gets the creature type delegate function pointer.
        /// </summary>
        static readonly GetCreatureTypeDelegate GetCreatureTypeFunction =
                    Marshal.GetDelegateForFunctionPointer<GetCreatureTypeDelegate>((IntPtr)MemoryAddresses.GetCreatureTypeFunPtr);

        /// <summary>
        /// Gets the creature type of the specified unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "unitPtr" as A
        /// participant "GetCreatureTypeFunction" as B
        /// participant "CreatureType" as C
        /// A -> B: GetCreatureTypeFunction(unitPtr)
        /// B --> C: return CreatureType
        /// \enduml
        /// </remarks>
        public CreatureType GetCreatureType(IntPtr unitPtr)
        {
            return (CreatureType)GetCreatureTypeFunction(unitPtr);
        }

        /// <summary>
        /// Retrieves a row from the item cache.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr ItemCacheGetRowDelegate(
                    IntPtr ptr,
                    int itemId,
                    IntPtr unknown,
                    int unused1,
                    int unused2,
                    char unused3);

        /// <summary>
        /// Gets the item cache entry function pointer.
        /// </summary>
        static readonly ItemCacheGetRowDelegate GetItemCacheEntryFunction =
                    Marshal.GetDelegateForFunctionPointer<ItemCacheGetRowDelegate>((IntPtr)MemoryAddresses.GetItemCacheEntryFunPtr);

        /// <summary>
        /// Retrieves the cache entry for a specific item.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Function Caller" as Caller
        /// participant "GetItemCacheEntry" as GetItem
        /// participant "GetItemCacheEntryFunction" as GetItemFunc
        /// 
        /// Caller -> GetItem: GetItemCacheEntry(itemId, guid)
        /// GetItem -> GetItemFunc: GetItemCacheEntryFunction((IntPtr)MemoryAddresses.ItemCacheEntryBasePtr, itemId, IntPtr.Zero, 0, 0, (char)0)
        /// GetItemFunc --> GetItem: return IntPtr
        /// GetItem --> Caller: return IntPtr
        /// \enduml
        /// </remarks>
        public IntPtr GetItemCacheEntry(int itemId, ulong guid)
        {
            return GetItemCacheEntryFunction((IntPtr)MemoryAddresses.ItemCacheEntryBasePtr, itemId, IntPtr.Zero, 0, 0, (char)0);
        }

        /// <summary>
        /// Represents a delegate that retrieves a pointer to an object based on a specified GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate IntPtr GetObjectPtrDelegate(ulong guid);

        /// <summary>
        /// Gets the function pointer for the GetObjectPtrDelegate and assigns it to the GetObjectPtrFunction.
        /// </summary>
        static readonly GetObjectPtrDelegate GetObjectPtrFunction =
                    Marshal.GetDelegateForFunctionPointer<GetObjectPtrDelegate>((IntPtr)MemoryAddresses.GetObjectPtrFunPtr);

        /// <summary>
        /// Retrieves the pointer to the object with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetObjectPtr" as G
        /// participant "GetObjectPtrFunction" as F
        /// 
        /// C -> G: GetObjectPtr(guid)
        /// G -> F: GetObjectPtrFunction(guid)
        /// F --> G: return IntPtr
        /// G --> C: return IntPtr
        /// \enduml
        /// </remarks>
        public IntPtr GetObjectPtr(ulong guid)
        {
            return GetObjectPtrFunction(guid);
        }

        /// <summary>
        /// Represents a delegate that retrieves the player's GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate ulong GetPlayerGuidDelegate();

        /// <summary>
        /// Gets the player GUID delegate function pointer.
        /// </summary>
        static GetPlayerGuidDelegate GetPlayerGuidFunction =
                    Marshal.GetDelegateForFunctionPointer<GetPlayerGuidDelegate>((IntPtr)MemoryAddresses.GetPlayerGuidFunPtr);

        /// <summary>
        /// Retrieves the player's GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Client -> GetPlayerGuid : Call GetPlayerGuid()
        /// GetPlayerGuid -> GetPlayerGuidFunction : Call GetPlayerGuidFunction()
        /// GetPlayerGuidFunction --> GetPlayerGuid : Return PlayerGuid
        /// GetPlayerGuid --> Client : Return PlayerGuid
        /// \enduml
        /// </remarks>
        public ulong GetPlayerGuid()
        {
            return GetPlayerGuidFunction();
        }

        /// <summary>
        /// Represents a delegate used to get a row from a specified pointer using an index and a buffer.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int GetRow2Delegate(IntPtr ptr, int index, byte[] buffer);

        /// <summary>
        /// Retrieves the GetRow2Delegate function pointer from the specified memory address.
        /// </summary>
        static readonly GetRow2Delegate GetRow2Function =
                    Marshal.GetDelegateForFunctionPointer<GetRow2Delegate>((IntPtr)MemoryAddresses.GetRow2FunPtr);

        /// <summary>
        /// Retrieves a Spell object from the spell database based on the specified index.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// GetSpellDBEntry -> GetRow2Function: index, buffer
        /// GetRow2Function --> GetSpellDBEntry: buffer
        /// GetSpellDBEntry -> MemoryManager: ReadString(spellNamePtr)
        /// MemoryManager --> GetSpellDBEntry: spellName
        /// GetSpellDBEntry -> MemoryManager: ReadString(spellDescriptionPtr)
        /// MemoryManager --> GetSpellDBEntry: spellDescription
        /// GetSpellDBEntry -> MemoryManager: ReadString(spellTooltipPtr)
        /// MemoryManager --> GetSpellDBEntry: spellTooltip
        /// GetSpellDBEntry --> Spell: new Spell(spellId, cost, spellName, spellDescription, spellTooltip)
        /// \enduml
        /// </remarks>
        public Spell GetSpellDBEntry(int index)
        {
            var buffer = new byte[0x260];

            GetRow2Function((IntPtr)0x00BA0BE0, index, buffer);

            var spellId = BitConverter.ToInt32(buffer, 0);
            var cost = BitConverter.ToInt32(buffer, 0x90);
            var spellNamePtr = BitConverter.ToInt32(buffer, 0x1FC);
            var spellName = "";
            if (spellNamePtr != 0)
                spellName = MemoryManager.ReadString((IntPtr)spellNamePtr);
            var spellDescriptionPtr = BitConverter.ToInt32(buffer, 0x204);
            var spellDescription = "";
            if (spellDescriptionPtr != 0)
                spellDescription = MemoryManager.ReadString((IntPtr)spellDescriptionPtr);
            var spellTooltipPtr = BitConverter.ToInt32(buffer, 0x208);
            var spellTooltip = "";
            if (spellTooltipPtr != 0)
                spellTooltip = MemoryManager.ReadString((IntPtr)spellTooltipPtr);

            return new Spell(spellId, cost, spellName, spellDescription, spellTooltip);
        }

        /// <summary>
        /// Represents a delegate that is used to get text.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate IntPtr GetTextDelegate(string arg);

        /// <summary>
        /// Gets the text delegate function pointer.
        /// </summary>
        static readonly GetTextDelegate GetTextFunction =
                    Marshal.GetDelegateForFunctionPointer<GetTextDelegate>((IntPtr)MemoryAddresses.GetTextFunPtr);

        /// <summary>
        /// Retrieves the text associated with the specified variable name.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetText" as GT
        /// C -> GT: GetText(varName)
        /// GT -> GT: GetTextFunction(varName)
        /// GT --> C: return IntPtr
        /// \enduml
        /// </remarks>
        public IntPtr GetText(string varName)
        {
            return GetTextFunction(varName);
        }

        /// <summary>
        /// Represents a delegate used to get the reaction of a unit.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int GetUnitReactionDelegate(IntPtr unitPtr1, IntPtr unitPtr2);

        /// <summary>
        /// Gets the unit reaction function pointer.
        /// </summary>
        static readonly GetUnitReactionDelegate GetUnitReactionFunction =
                    Marshal.GetDelegateForFunctionPointer<GetUnitReactionDelegate>((IntPtr)MemoryAddresses.GetUnitReactionFunPtr);

        /// <summary>
        /// Gets the reaction between two units.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor "unitPtr1" as a
        /// actor "unitPtr2" as b
        /// a -> b: GetUnitReactionFunction(unitPtr1, unitPtr2)
        /// b --> a: return UnitReaction
        /// \enduml
        /// </remarks>
        public UnitReaction GetUnitReaction(IntPtr unitPtr1, IntPtr unitPtr2)
        {
            return (UnitReaction)GetUnitReactionFunction(unitPtr1, unitPtr2);
        }

        /// <summary>
        /// Represents a delegate for the Intersect function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int IntersectDelegate(ref XYZ start, ref XYZ end, ref XYZ intersection, ref float distance, uint flags, IntPtr Ptr);

        /// <summary>
        /// Gets the delegate for the Intersect function pointer.
        /// </summary>
        static readonly IntersectDelegate IntersectFunction =
                    Marshal.GetDelegateForFunctionPointer<IntersectDelegate>((IntPtr)MemoryAddresses.IntersectFunPtr);

        /// <summary>
        /// Calculates the intersection point between two positions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Position -> XYZ: start
        /// Position -> XYZ: end
        /// XYZ -> XYZ: intersection
        /// Position -> Position: DistanceTo(end)
        /// Position -> XYZ: startXYZ
        /// Position -> XYZ: endXYZ
        /// XYZ -> XYZ: IntersectFunction(startXYZ, endXYZ, intersection, distance, 0x00100171, IntPtr.Zero)
        /// XYZ --> XYZ: intersection
        /// \enduml
        /// </remarks>
        public XYZ Intersect(Position start, Position end)
        {
            var intersection = new XYZ();
            var distance = start.DistanceTo(end);
            var startXYZ = new XYZ(start.X, start.Y, start.Z + 5.0f);
            var endXYZ = new XYZ(end.X, end.Y, end.Z + 5.0f);

            IntersectFunction(ref startXYZ, ref endXYZ, ref intersection, ref distance, 0x00100171, IntPtr.Zero);

            return intersection;
        }

        /// <summary>
        /// Represents a delegate used to check if a spell is on cooldown.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate bool IsSpellOnCooldownDelegate(IntPtr ptr, int spellId, int unknown1, int unknown2, int unknown3, int unknown4);

        /// <summary>
        /// Gets a delegate for the IsSpellOnCooldown function pointer.
        /// </summary>
        static readonly IsSpellOnCooldownDelegate IsSpellOnCooldownFunction =
                    Marshal.GetDelegateForFunctionPointer<IsSpellOnCooldownDelegate>((IntPtr)MemoryAddresses.IsSpellOnCooldownFunPtr);

        /// <summary>
        /// Checks if a spell is currently on cooldown.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "IsSpellOnCooldown Function" as A
        /// participant "IsSpellOnCooldown Method" as B
        /// B -> A: IsSpellOnCooldown(spellId)
        /// A --> B: return
        /// \enduml
        /// </remarks>
        public bool IsSpellOnCooldown(int spellId)
        {
            return IsSpellOnCooldownFunction((IntPtr)0x00E1D7F4, spellId, 0, 0, 0, 0);
        }

        /// <summary>
        /// Represents a delegate for a function that jumps to a specific location in memory.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate int JumpDelegate();

        /// <summary>
        /// Gets the delegate for the jump function pointer.
        /// </summary>
        static readonly JumpDelegate JumpFunction =
                    Marshal.GetDelegateForFunctionPointer<JumpDelegate>((IntPtr)MemoryAddresses.JumpFunPtr);

        /// <summary>
        /// Calls the JumpFunction method.
        /// </summary>
        /// <remarks>
        /// \startuml
        ///  participant "Jump()" as J
        ///  participant "JumpFunction()" as F
        ///  J -> F: Calls JumpFunction
        /// \enduml
        /// </remarks>
        public void Jump()
        {
            JumpFunction();
        }

        /// <summary>
        /// Represents a delegate that is used to handle loot slot events.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void LootSlotDelegate(int slot);

        /// <summary>
        /// Retrieves the loot slot delegate function pointer from the specified memory address.
        /// </summary>
        static readonly LootSlotDelegate LootSlotFunction =
                    Marshal.GetDelegateForFunctionPointer<LootSlotDelegate>((IntPtr)MemoryAddresses.LootSlotFunPtr);

        /// <summary>
        /// Loots the specified slot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "function LootSlot" as A
        /// participant "function LootSlotFunction" as B
        /// A -> B: slot
        /// \enduml
        /// </remarks>
        public void LootSlot(int slot)
        {
            LootSlotFunction(slot);
        }

        /// <summary>
        /// Represents a delegate for calling Lua code with two string parameters and an integer parameter.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate int LuaCallDelegate(string code1, string code2, int unknown);

        /// <summary>
        /// Gets the Lua call delegate for the specified function pointer.
        /// </summary>
        static readonly LuaCallDelegate LuaCallFunction =
                    Marshal.GetDelegateForFunctionPointer<LuaCallDelegate>((IntPtr)MemoryAddresses.LuaCallFunPtr);

        /// <summary>
        /// Calls a Lua function with the specified code.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "LuaCall Function" as L
        /// L -> L: LuaCall(code)
        /// L -> L: LuaCallFunction(code, code, 0)
        /// \enduml
        /// </remarks>
        public void LuaCall(string code)
        {
            LuaCallFunction(code, code, 0);
        }

        /// <summary>
        /// Represents a delegate for releasing a corpse.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int ReleaseCorpseDelegate(IntPtr ptr);

        /// <summary>
        /// Gets the delegate for releasing a corpse.
        /// </summary>
        static readonly ReleaseCorpseDelegate ReleaseCorpseFunction =
                    Marshal.GetDelegateForFunctionPointer<ReleaseCorpseDelegate>((IntPtr)MemoryAddresses.ReleaseCorpseFunPtr);

        /// <summary>
        /// Releases the memory allocated for a corpse object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Function Caller" as A
        /// participant "ReleaseCorpse" as B
        /// A -> B: ReleaseCorpse(ptr)
        /// B -> B: ReleaseCorpseFunction(ptr)
        /// \enduml
        /// </remarks>
        public void ReleaseCorpse(IntPtr ptr)
        {
            ReleaseCorpseFunction(ptr);
        }

        /// <summary>
        /// Represents a delegate that retrieves a corpse and returns an integer value.
        /// </summary>
        delegate int RetrieveCorpseDelegate();

        /// <summary>
        /// Retrieves the delegate function pointer for retrieving a corpse.
        /// </summary>
        static readonly RetrieveCorpseDelegate RetrieveCorpseFunction =
                    Marshal.GetDelegateForFunctionPointer<RetrieveCorpseDelegate>((IntPtr)MemoryAddresses.RetrieveCorpseFunPtr);

        /// <summary>
        /// Retrieves the corpse.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// RetrieveCorpse -> RetrieveCorpseFunction : call
        /// \enduml
        /// </remarks>
        public void RetrieveCorpse()
        {
            RetrieveCorpseFunction();
        }

        /// <summary>
        /// Represents a delegate that sets the target with a specified GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void SetTargetDelegate(ulong guid);

        /// <summary>
        /// Gets the delegate for the SetTargetFunction pointer.
        /// </summary>
        static readonly SetTargetDelegate SetTargetFunction =
                    Marshal.GetDelegateForFunctionPointer<SetTargetDelegate>((IntPtr)MemoryAddresses.SetTargetFunPtr);

        /// <summary>
        /// Sets the target using the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "SetTarget" as ST
        /// C -> ST: SetTarget(guid)
        /// ST -> ST: SetTargetFunction(guid)
        /// \enduml
        /// </remarks>
        public void SetTarget(ulong guid)
        {
            SetTargetFunction(guid);
        }

        /// <summary>
        /// Delegate for selling an item by its GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void SellItemByGuidDelegate(ulong vendorGuid, ulong itemGuid, int unused);

        /// <summary>
        /// Gets the SellItemByGuidDelegate function pointer from the specified memory address and assigns it to the SellItemByGuidFunction delegate.
        /// </summary>
        static readonly SellItemByGuidDelegate SellItemByGuidFunction =
                    Marshal.GetDelegateForFunctionPointer<SellItemByGuidDelegate>((IntPtr)MemoryAddresses.SellItemByGuidFunPtr);

        /// <summary>
        /// Sells an item by its GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Function : SellItemByGuid" as F
        /// participant "Function : SellItemByGuidFunction" as S
        /// F -> S: SellItemByGuidFunction(vendorGuid, itemGuid, 0)
        /// \enduml
        /// </remarks>
        public void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid)
        {
            SellItemByGuidFunction(vendorGuid, itemGuid, 0);
        }

        /// <summary>
        /// Delegate for sending movement update.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void SendMovementUpdateDelegate(
                    IntPtr playerPtr,
                    IntPtr unknown,
                    int OpCode,
                    int unknown2,
                    int unknown3);

        /// <summary>
        /// Retrieves the function pointer for sending movement updates and assigns it to the SendMovementUpdateFunction delegate.
        /// </summary>
        static readonly SendMovementUpdateDelegate SendMovementUpdateFunction =
                    Marshal.GetDelegateForFunctionPointer<SendMovementUpdateDelegate>((IntPtr)MemoryAddresses.SendMovementUpdateFunPtr);

        /// <summary>
        /// Sends a movement update to the specified player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "playerPtr" as A
        /// participant "SendMovementUpdateFunction" as B
        /// A -> B: SendMovementUpdateFunction(playerPtr, (IntPtr)0x00BE1E2C, opcode, 0, 0)
        /// \enduml
        /// </remarks>
        public void SendMovementUpdate(IntPtr playerPtr, int opcode)
        {
            SendMovementUpdateFunction(playerPtr, (IntPtr)0x00BE1E2C, opcode, 0, 0);
        }

        /// <summary>
        /// Represents a delegate used to set a control bit on a device.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void SetControlBitDelegate(IntPtr device, int bit, int state, int tickCount);

        /// <summary>
        /// Gets the delegate for the SetControlBitFunction.
        /// </summary>
        static readonly SetControlBitDelegate SetControlBitFunction =
                    Marshal.GetDelegateForFunctionPointer<SetControlBitDelegate>((IntPtr)MemoryAddresses.SetControlBitFunPtr);

        /// <summary>
        /// Sets the control bit of a device.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SetControlBit Function" as SCB
        /// participant "MemoryManager" as MM
        /// participant "SetControlBitDevicePtr" as SCDP
        /// 
        /// SCB -> MM: ReadIntPtr((IntPtr)MemoryAddresses.SetControlBitDevicePtr)
        /// activate MM
        /// MM --> SCB: ptr
        /// deactivate MM
        /// 
        /// SCB -> SCB: SetControlBitFunction(ptr, bit, state, tickCount)
        /// \enduml
        /// </remarks>
        public void SetControlBit(int bit, int state, int tickCount)
        {
            var ptr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.SetControlBitDevicePtr);
            SetControlBitFunction(ptr, bit, state, tickCount);
        }

        /// <summary>
        /// Delegate for setting the facing direction of a player.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void SetFacingDelegate(IntPtr playerSetFacingPtr, float facing);

        /// <summary>
        /// Gets the delegate for the SetFacingFunction pointer.
        /// </summary>
        static readonly SetFacingDelegate SetFacingFunction =
                    Marshal.GetDelegateForFunctionPointer<SetFacingDelegate>((IntPtr)MemoryAddresses.SetFacingFunPtr);

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor "Caller" as c
        /// participant "SetFacing" as sf
        /// c -> sf : SetFacing(playerSetFacingPtr, facing)
        /// sf -> sf : SetFacingFunction(playerSetFacingPtr, facing)
        /// \enduml
        /// </remarks>
        public void SetFacing(IntPtr playerSetFacingPtr, float facing)
        {
            SetFacingFunction(playerSetFacingPtr, facing);
        }

        /// <summary>
        /// Represents a delegate that is used to invoke the UseItem method.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void UseItemDelegate(IntPtr itemPtr, ref ulong unused1, int unused2);

        /// <summary>
        /// Gets the delegate for the UseItemFunction pointer.
        /// </summary>
        static readonly UseItemDelegate UseItemFunction =
                    Marshal.GetDelegateForFunctionPointer<UseItemDelegate>((IntPtr)MemoryAddresses.UseItemFunPtr);

        /// <summary>
        /// Uses the specified item.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> UseItem : itemPtr
        /// UseItem -> UseItemFunction : itemPtr, unused1, 0
        /// \enduml
        /// </remarks>
        public void UseItem(IntPtr itemPtr)
        {
            ulong unused1 = 0;
            UseItemFunction(itemPtr, ref unused1, 0);
        }

        /// <summary>
        /// Retrieves a pointer to the specified row in the table.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> GetRow: tablePtr, index
        /// note right: User calls GetRow with a tablePtr and an index.
        /// GetRow -> NotImplementedException: throw
        /// note right: GetRow throws NotImplementedException.
        /// \enduml
        /// </remarks>
        public IntPtr GetRow(IntPtr tablePtr, int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the localized row from the specified table at the given index.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> GetLocalizedRow : tablePtr, index, rowPtr
        /// GetLocalizedRow --> User : NotImplementedException
        /// \enduml
        /// </remarks>
        public IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the aura count for the specified unit pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetAuraCount Function" as F
        /// C -> F: unitPtr
        /// note right: Function not implemented yet
        /// \enduml
        /// </remarks>
        public int GetAuraCount(IntPtr unitPtr)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the pointer to the aura at the specified index for the given unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "unitPtr" as A
        /// participant "GetAuraPointer" as B
        /// participant "index" as C
        /// A -> B: Request Aura Pointer
        /// B -> C: Pass index
        /// note right: Not Implemented Exception
        /// \enduml
        /// </remarks>
        public IntPtr GetAuraPointer(IntPtr unitPtr, int index)
        {
            throw new NotImplementedException();
        }
    }
}
