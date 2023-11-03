using BloogBot.Game.Enums;
using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

/// <summary>
/// This class handles the buying of items from vendors in the game.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// This class handles the game functions for a vanilla game.
    /// </summary>
    public class VanillaGameFunctionHandler : IGameFunctionHandler
    {
        /// <summary>
        /// Calls the BuyVendorItem function from the FastCall.dll library to purchase a vendor item.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "BuyVendorItem")]
        static extern void BuyVendorItemFunction(int itemId, int quantity, ulong vendorGuid, IntPtr ptr);

        /// <summary>
        /// Buys a vendor item with the specified vendor GUID, item ID, and quantity.
        /// </summary>
        public void BuyVendorItem(ulong vendorGuid, int itemId, int quantity)
        {
            BuyVendorItemFunction(itemId, quantity, vendorGuid, (IntPtr)MemoryAddresses.BuyVendorItemFunPtr);
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
        public void CastAtPosition(string spellName, Position position)
        {
            MemoryManager.WriteByte((IntPtr)0xCECAC0, 0);
            LuaCall($"CastSpellByName('{spellName}')");
            var pos = position.ToXYZ();
            CastAtPositionFunction(ref pos);
        }

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        public int CastSpellById(int spellId, ulong targetGuid)
        {
            // not used in vanilla
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dismounts the specified unit.
        /// </summary>
        public int Dismount(IntPtr unitPtr)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls the EnumerateVisibleObjects function from the FastCall.dll library.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "EnumerateVisibleObjects")]
        static extern void EnumerateVisibleObjectsFunction(IntPtr callback, int filter, IntPtr ptr);

        /// <summary>
        /// Enumerates the visible objects using the specified callback and filter.
        /// </summary>
        // what does this do? [HandleProcessCorruptedStateExceptions]
        public void EnumerateVisibleObjects(IntPtr callback, int filter)
        {
            EnumerateVisibleObjectsFunction(callback, filter, (IntPtr)MemoryAddresses.EnumerateVisibleObjectsFunPtr);
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
        /// Retrieves the item cache entry function pointer.
        /// </summary>
        static readonly ItemCacheGetRowDelegate GetItemCacheEntryFunction =
                    Marshal.GetDelegateForFunctionPointer<ItemCacheGetRowDelegate>((IntPtr)MemoryAddresses.GetItemCacheEntryFunPtr);

        /// <summary>
        /// Retrieves the cache entry for a specific item.
        /// </summary>
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
        public IntPtr GetObjectPtr(ulong guid)
        {
            return GetObjectPtrFunction(guid);
        }

        /// <summary>
        /// Represents a delegate that retrieves the player's GUID.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate ulong GetPlayerGuidDelegate();

        /// <summary>
        /// Gets the player GUID delegate function pointer.
        /// </summary>
        static GetPlayerGuidDelegate GetPlayerGuidFunction =
                    Marshal.GetDelegateForFunctionPointer<GetPlayerGuidDelegate>((IntPtr)MemoryAddresses.GetPlayerGuidFunPtr);

        /// <summary>
        /// Retrieves the player's GUID.
        /// </summary>
        public ulong GetPlayerGuid()
        {
            return GetPlayerGuidFunction();
        }

        /// <summary>
        /// Retrieves the spell database entry at the specified index.
        /// </summary>
        public Spell GetSpellDBEntry(int index)
        {
            // we don't use this in Vanilla, because we can get the spell entry directly from a static memory address
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls the GetText function from the FastCall.dll library, passing a variable name and a pointer, and returns an IntPtr.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "GetText")]
        static extern IntPtr GetTextFunction(string varName, IntPtr ptr);

        /// <summary>
        /// Retrieves the text associated with the specified variable name.
        /// </summary>
        public IntPtr GetText(string varName)
        {
            return GetTextFunction(varName, (IntPtr)MemoryAddresses.GetTextFunPtr);
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
        /// Calls the Intersect2 function from the FastCall.dll library to calculate the intersection point and distance between two XYZ points.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "Intersect2")]
        static extern bool IntersectFunction(ref XYZ p1, ref XYZ p2, ref XYZ intersection, ref float distance, uint flags, IntPtr Ptr);

        /// <summary>
        /// Returns { 1, 1, 1 } if there is a collission when casting a ray between start and end params.
        /// A result of { 1, 1, 1 } would indicate you are not in line-of-sight with your target.
        /// </summary>
        /// <param name="start">The start of the raycast.</param>
        /// <param name="end">The end of the raycast.</param>
        /// <returns>The result of the collision check.</returns>
        public XYZ Intersect(Position start, Position end)
        {
            var intersection = new XYZ();
            var distance = start.DistanceTo(end);
            var p1 = new XYZ(start.X, start.Y, start.Z + 2);
            var p2 = new XYZ(end.X, end.Y, end.Z + 2);

            var result = IntersectFunction(ref p1, ref p2, ref intersection, ref distance, 0x00100111, (IntPtr)MemoryAddresses.IntersectFunPtr);

            var collisionDetected = result && distance < 1;

            return collisionDetected ? new XYZ(1, 1, 1) : new XYZ(0, 0, 0);
        }

        /// <summary>
        /// Represents a delegate for checking if a spell is on cooldown.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void IsSpellOnCooldownDelegate(
                    IntPtr spellCooldownPtr,
                    int spellId,
                    int unused1,
                    ref int cooldownDuration,
                    int unused2,
                    bool unused3);

        /// <summary>
        /// Gets a delegate for the IsSpellOnCooldown function pointer.
        /// </summary>
        static readonly IsSpellOnCooldownDelegate IsSpellOnCooldownFunction =
                    Marshal.GetDelegateForFunctionPointer<IsSpellOnCooldownDelegate>((IntPtr)MemoryAddresses.IsSpellOnCooldownFunPtr);

        /// <summary>
        /// Checks if a spell is currently on cooldown.
        /// </summary>
        public bool IsSpellOnCooldown(int spellId)
        {
            var cooldownDuration = 0;
            IsSpellOnCooldownFunction(
                (IntPtr)0x00CECAEC,
                spellId,
                0,
                ref cooldownDuration,
                0,
                false);

            return cooldownDuration != 0;
        }

        /// <summary>
        /// Jumps to the next position.
        /// </summary>
        public void Jump()
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls the LootSlotFunction from the FastCall.dll library to retrieve the loot from a specified slot.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "LootSlot")]
        static extern byte LootSlotFunction(int slot, IntPtr ptr);

        /// <summary>
        /// Loots the specified slot.
        /// </summary>
        public void LootSlot(int slot)
        {
            LootSlotFunction(slot, (IntPtr)MemoryAddresses.LootSlotFunPtr);
        }

        /// <summary>
        /// Calls a Lua function with the specified code and pointer.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "LuaCall")]
        static extern void LuaCallFunction(string code, int ptr);

        /// <summary>
        /// Calls a Lua function with the given code.
        /// </summary>
        public void LuaCall(string code)
        {
            LuaCallFunction(code, MemoryAddresses.LuaCallFunPtr);
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
        /// Releases a corpse by calling the ReleaseCorpseFunction with the specified pointer. If an AccessViolationException occurs, it is caught and a message is printed to the console indicating that this is most likely a transient error. The bot should continue trying to release and recover from this error.
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        public void ReleaseCorpse(IntPtr ptr)
        {
            try
            {
                ReleaseCorpseFunction(ptr);
            }
            catch (AccessViolationException)
            {
                Console.WriteLine("AccessViolationException occurred while trying to release corpse. Most likely, this is due to a transient error that caused the player pointer to temporarily equal IntPtr.Zero. The bot should keep trying to release and recover from this error.");
            }
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
        public void SetTarget(ulong guid)
        {
            SetTargetFunction(guid);
        }

        /// <summary>
        /// Calls the SellItemByGuid function from the FastCall.dll library to sell an item by its GUID.
        /// </summary>
        [DllImport("FastCall.dll", EntryPoint = "SellItemByGuid")]
        static extern void SellItemByGuidFunction(uint itemCount, ulong npcGuid, ulong itemGuid, IntPtr sellItemFunPtr);

        /// <summary>
        /// Sells an item by its unique identifier.
        /// </summary>
        public void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid)
        {
            SellItemByGuidFunction(itemCount, vendorGuid, itemGuid, (IntPtr)MemoryAddresses.SellItemByGuidFunPtr);
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
        delegate void SetFacingDelegate(IntPtr playerSetFacingPtr, float facing);

        /// <summary>
        /// Gets the delegate for the SetFacingFunction pointer.
        /// </summary>
        static readonly SetFacingDelegate SetFacingFunction =
                    Marshal.GetDelegateForFunctionPointer<SetFacingDelegate>((IntPtr)MemoryAddresses.SetFacingFunPtr);

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
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
        public void UseItem(IntPtr itemPtr)
        {
            ulong unused1 = 0;
            UseItemFunction(itemPtr, ref unused1, 0);
        }

        /// <summary>
        /// Retrieves a pointer to the specified row in the table.
        /// </summary>
        public IntPtr GetRow(IntPtr tablePtr, int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the localized row from the specified table at the given index.
        /// </summary>
        public IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the aura count for the specified unit pointer.
        /// </summary>
        public int GetAuraCount(IntPtr unitPtr)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the pointer to the aura at the specified index for the given unit.
        /// </summary>
        public IntPtr GetAuraPointer(IntPtr unitPtr, int index)
        {
            throw new NotImplementedException();
        }
    }
}
