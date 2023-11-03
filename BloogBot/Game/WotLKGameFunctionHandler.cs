using BloogBot.Game.Enums;
using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains the game function handler for the Wrath of the Lich King expansion.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a handler for game functions specific to the Wrath of the Lich King expansion.
    /// </summary>
    /// <summary>
    /// Represents a game function handler for the Wrath of the Lich King expansion.
    /// </summary>
    public class WotLKGameFunctionHandler : IGameFunctionHandler
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
        public void BuyVendorItem(ulong vendorGuid, int itemId, int quantity)
        {
            BuyVendorItemFunction(vendorGuid, itemId, quantity, 1);
        }

        /// <summary>
        /// Casts a spell at the specified position.
        /// </summary>
        public void CastAtPosition(string spellName, Position position)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Represents a delegate for casting a spell by its ID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int CastSpellByIdDelegate(int spellId, int itemId, ulong guid, int isTrade, int a6, int a7, int a8);

        /// <summary>
        /// Retrieves the function pointer for the CastSpellByIdDelegate and assigns it to the CastSpellByIdFunction.
        /// </summary>
        static readonly CastSpellByIdDelegate CastSpellByIdFunction =
                    Marshal.GetDelegateForFunctionPointer<CastSpellByIdDelegate>((IntPtr)MemoryAddresses.CastSpellByIdFunPtr);

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        public int CastSpellById(int spellId, ulong targetGuid)
        {
            return CastSpellByIdFunction(spellId, 0, targetGuid, 0, 0, 0, 0);
        }

        /// <summary>
        /// Dismounts the unit.
        /// </summary>
        public int Dismount(IntPtr unitPtr)
        {
            LuaCall("Dismount()");

            return 0;
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
        public CreatureType GetCreatureType(IntPtr unitPtr)
        {
            return (CreatureType)GetCreatureTypeFunction(unitPtr);
        }

        /// <summary>
        /// Retrieves a row from the item cache based on the specified item ID and GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr ItemCacheGetRowDelegate(
                    IntPtr ptr,
                    int itemId,
                    ref ulong guid,
                    int unused1,
                    int unused2,
                    int unused3);

        /// <summary>
        /// Gets the item cache entry function pointer.
        /// </summary>
        static readonly ItemCacheGetRowDelegate GetItemCacheEntryFunction =
                    Marshal.GetDelegateForFunctionPointer<ItemCacheGetRowDelegate>((IntPtr)MemoryAddresses.GetItemCacheEntryFunPtr);

        /// <summary>
        /// Retrieves the cache entry for a specific item.
        /// </summary>
        public IntPtr GetItemCacheEntry(int itemId, ulong guid)
        {
            return GetItemCacheEntryFunction((IntPtr)MemoryAddresses.ItemCacheEntryBasePtr, itemId, ref guid, 0, 0, 0);
        }

        /// <summary>
        /// Represents a delegate that retrieves a pointer to an object based on its GUID, type mask, file, and line.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetObjectPtrDelegate(ulong objectGuid, uint typeMask, string file, int line);

        /// <summary>
        /// Gets the function pointer for the GetObjectPtrDelegate and assigns it to the GetObjectPtrFunction.
        /// </summary>
        static readonly GetObjectPtrDelegate GetObjectPtrFunction =
                    Marshal.GetDelegateForFunctionPointer<GetObjectPtrDelegate>((IntPtr)MemoryAddresses.GetObjectPtrFunPtr);

        /// <summary>
        /// Retrieves a pointer to an object based on the specified GUID.
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        public IntPtr GetObjectPtr(ulong guid)
        {
            return GetObjectPtrFunction(guid, 0xFFFFFFFF, string.Empty, 0);
        }

        /// <summary>
        /// Retrieves the player's GUID.
        /// </summary>
        public ulong GetPlayerGuid()
        {
            return MemoryManager.ReadUlong((IntPtr)0x00CA1238);
        }

        /// <summary>
        /// Retrieves a Spell object from the spell database based on the specified index.
        /// </summary>
        public Spell GetSpellDBEntry(int index)
        {
            var spellPtr = WowDb.Tables[ClientDb.Spell].GetLocalizedRow(index);

            var costAddr = IntPtr.Add(spellPtr, 0xA8);
            var cost = MemoryManager.ReadInt(costAddr);

            var spellNamePtrAddr = IntPtr.Add(spellPtr, 0x220);
            var spellNamePtr = MemoryManager.ReadIntPtr(spellNamePtrAddr);
            var name = MemoryManager.ReadString(spellNamePtr);

            var spellDescriptionPtrAddr = IntPtr.Add(spellPtr, 0x228);
            var spellDescriptionPtr = MemoryManager.ReadIntPtr(spellDescriptionPtrAddr);
            var description = MemoryManager.ReadString(spellDescriptionPtr);

            var spellTooltipPtrAddr = IntPtr.Add(spellPtr, 0x22C);
            var spellTooltipPtr = MemoryManager.ReadIntPtr(spellTooltipPtrAddr);
            var tooltip = MemoryManager.ReadString(spellTooltipPtr);

            Marshal.FreeHGlobal(spellPtr);

            return new Spell(index, cost, name, description, tooltip);
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
        /// Represents a delegate for getting the cooldown of a spell.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool GetSpellCooldownDelegate(int spellId, bool isPet, ref int duration, ref int start, ref bool isReady, ref int unk0);

        /// <summary>
        /// Retrieves the cooldown of a spell using the specified delegate function pointer.
        /// </summary>
        static readonly GetSpellCooldownDelegate GetSpellCooldownFunction =
                    Marshal.GetDelegateForFunctionPointer<GetSpellCooldownDelegate>((IntPtr)MemoryAddresses.IsSpellOnCooldownFunPtr);

        /// <summary>
        /// Checks if a spell with the specified ID is currently on cooldown.
        /// </summary>
        public bool IsSpellOnCooldown(int spellId)
        {
            var duration = 0;
            var start = 0;
            var isReady = false;
            var unk0 = 0;
            GetSpellCooldownFunction(spellId, false, ref duration, ref start, ref isReady, ref unk0);

            var result = start + duration - (int)PerformanceCounter();
            var cooldown = isReady ? (result > 0 ? result / 1000f : 0f) : float.MaxValue;

            return cooldown > 0;
        }

        /// <summary>
        /// Calls the Lua function "JumpOrAscendStart()" to make the character jump or start ascending.
        /// </summary>
        public void Jump()
        {
            LuaCall("JumpOrAscendStart()");
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
        public void LuaCall(string code)
        {
            LuaCallFunction(code, code, 0);
        }

        /// <summary>
        /// Releases a corpse with the specified pointer.
        /// </summary>
        public void ReleaseCorpse(IntPtr ptr)
        {
            LuaCall("RepopMe()");
        }

        /// <summary>
        /// Retrieves the player's corpse.
        /// </summary>
        public void RetrieveCorpse()
        {
            LuaCall("RetrieveCorpse()");
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
        public void SetControlBit(int bit, int state, int tickCount)
        {
            var ptr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.SetControlBitDevicePtr);
            SetControlBitFunction(ptr, bit, state, tickCount);
        }

        /// <summary>
        /// Delegate for setting the facing direction of a player.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void SetFacingDelegate(IntPtr playerSetFacingPtr, uint time, float facing);

        /// <summary>
        /// Gets the delegate for the SetFacingFunction pointer.
        /// </summary>
        static readonly SetFacingDelegate SetFacingFunction =
                    Marshal.GetDelegateForFunctionPointer<SetFacingDelegate>((IntPtr)MemoryAddresses.SetFacingFunPtr);

        /// <summary>
        /// Represents a delegate for a performance counter function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint PerformanceCounterDelegate();

        /// <summary>
        /// Gets the performance counter delegate for the specified function pointer.
        /// </summary>
        static readonly PerformanceCounterDelegate PerformanceCounter =
                    Marshal.GetDelegateForFunctionPointer<PerformanceCounterDelegate>((IntPtr)0x0086AE20);

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        public void SetFacing(IntPtr playerSetFacingPtr, float facing)
        {
            var performanceCounter = PerformanceCounter();
            SetFacingFunction(playerSetFacingPtr, performanceCounter, facing);
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
        public void UseItem(IntPtr itemPtr)
        {
            ulong unused1 = 0;
            UseItemFunction(itemPtr, ref unused1, 0);
        }

        /// <summary>
        /// Represents a delegate that is used to get a row from a table at a specified index.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr GetRowDelegate(IntPtr tablePtr, int index);

        /// <summary>
        /// Retrieves the row delegate function pointer from the specified memory address.
        /// </summary>
        static readonly GetRowDelegate GetRowFunction =
                    Marshal.GetDelegateForFunctionPointer<GetRowDelegate>((IntPtr)MemoryAddresses.GetRowFunPtr);

        /// <summary>
        /// Retrieves a pointer to the row at the specified index in the table.
        /// </summary>
        public IntPtr GetRow(IntPtr tablePtr, int index)
        {
            return GetRowFunction(tablePtr, index);
        }

        /// <summary>
        /// Represents a delegate used to get a localized row from a table.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr GetLocalizedRowDelegate(IntPtr tablePtr, int index, IntPtr rowPtr);

        /// <summary>
        /// Gets the localized row using the specified function pointer.
        /// </summary>
        static readonly GetLocalizedRowDelegate GetLocalizedRowFunction =
                    Marshal.GetDelegateForFunctionPointer<GetLocalizedRowDelegate>((IntPtr)MemoryAddresses.GetLocalizedRowFunPtr);

        /// <summary>
        /// Retrieves a localized row from a table at the specified index.
        /// </summary>
        public IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr)
        {
            return GetLocalizedRowFunction(tablePtr, index, rowPtr);
        }

        /// <summary>
        /// Represents a delegate for the GetAuraCount function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int GetAuraCountDelegate(IntPtr thisObj);

        /// <summary>
        /// Retrieves the count of auras using the specified delegate function pointer.
        /// </summary>
        static readonly GetAuraCountDelegate GetAuraCountFunction =
                    Marshal.GetDelegateForFunctionPointer<GetAuraCountDelegate>((IntPtr)MemoryAddresses.GetAuraCountFunPtr);

        /// <summary>
        /// Gets the count of auras for a given unit.
        /// </summary>
        public int GetAuraCount(IntPtr unitPtr)
        {
            return GetAuraCountFunction(unitPtr);
        }

        /// <summary>
        /// Represents a delegate used to get the aura at a specific index for a given unit.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr GetAuraDelegate(IntPtr unitPtr, int index);

        /// <summary>
        /// Gets the delegate for the GetAuraFunction pointer.
        /// </summary>
        static readonly GetAuraDelegate GetAuraFunction =
                    Marshal.GetDelegateForFunctionPointer<GetAuraDelegate>((IntPtr)MemoryAddresses.GetAuraFunPtr);

        /// <summary>
        /// Retrieves the pointer to the aura at the specified index for the given unit.
        /// </summary>
        public IntPtr GetAuraPointer(IntPtr unitPtr, int index)
        {
            return GetAuraFunction(unitPtr, index);
        }
    }
}
