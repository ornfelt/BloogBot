using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

/// <summary>
/// This namespace contains the ObjectManager class which handles the management of game objects.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// The ObjectManager class is responsible for managing and manipulating game objects.
    /// </summary>
    public class ObjectManager
    {
        /// <summary>
        /// The offset value for the object type.
        /// </summary>
        const int OBJECT_TYPE_OFFSET = 0x14;

        /// <summary>
        /// Represents a callback function used to enumerate visible objects.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate int EnumerateVisibleObjectsCallbackVanilla(int filter, ulong guid);

        /// <summary>
        /// Represents a callback function used to enumerate visible objects.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int EnumerateVisibleObjectsCallbackNonVanilla(ulong guid, int filter);

        /// <summary>
        /// Represents the unique identifier for a player.
        /// </summary>
        static ulong playerGuid;
        /// <summary>
        /// Represents a static variable of type EnumerateVisibleObjectsCallbackVanilla.
        /// </summary>
        static EnumerateVisibleObjectsCallbackVanilla callbackVanilla;
        /// <summary>
        /// Represents a static callback for enumerating visible objects that are non-vanilla.
        /// </summary>
        static EnumerateVisibleObjectsCallbackNonVanilla callbackNonVanilla;
        /// <summary>
        /// The pointer to the callback function.
        /// </summary>
        static IntPtr callbackPtr;
        /// <summary>
        /// Represents a static instance of the Probe class.
        /// </summary>
        static Probe probe;

        /// <summary>
        /// Represents a list of World of Warcraft objects.
        /// </summary>
        static internal IList<WoWObject> Objects = new List<WoWObject>();
        /// <summary>
        /// The buffer that stores a list of WoWObjects.
        /// </summary>
        static internal IList<WoWObject> ObjectsBuffer = new List<WoWObject>();
        /// <summary>
        /// Gets or sets a value indicating whether the killswitch has been triggered.
        /// </summary>
        static internal bool KillswitchTriggered;

        /// <summary>
        /// Initializes the probe with the given parameter.
        /// </summary>
        static internal void Initialize(Probe parProbe)
        {
            probe = parProbe;

            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                callbackVanilla = CallbackVanilla;
                callbackPtr = Marshal.GetFunctionPointerForDelegate(callbackVanilla);
            }
            else
            {
                callbackNonVanilla = CallbackNonVanilla;
                callbackPtr = Marshal.GetFunctionPointerForDelegate(callbackNonVanilla);
            }

        }

        /// <summary>
        /// Gets or sets the local player.
        /// </summary>
        static public LocalPlayer Player { get; private set; }

        /// <summary>
        /// Gets or sets the local pet.
        /// </summary>
        static public LocalPet Pet { get; private set; }

        /// <summary>
        /// Gets all the WoWObjects.
        /// </summary>
        static public IEnumerable<WoWObject> AllObjects => Objects;

        /// <summary>
        /// Gets a collection of WoWUnit objects that are filtered by ObjectType.Unit.
        /// </summary>
        static public IEnumerable<WoWUnit> Units => Objects.OfType<WoWUnit>().Where(o => o.ObjectType == ObjectType.Unit).ToList();

        /// <summary>
        /// Gets an enumerable collection of WoWPlayer objects.
        /// </summary>
        static public IEnumerable<WoWPlayer> Players => Objects.OfType<WoWPlayer>();

        /// <summary>
        /// Gets an enumerable collection of WoWItem objects.
        /// </summary>
        static public IEnumerable<WoWItem> Items => Objects.OfType<WoWItem>();

        /// <summary>
        /// Gets all the WoWContainers from the Objects collection.
        /// </summary>
        static public IEnumerable<WoWContainer> Containers => Objects.OfType<WoWContainer>();

        /// <summary>
        /// Gets all the WoWGameObjects from the Objects collection.
        /// </summary>
        static public IEnumerable<WoWGameObject> GameObjects => Objects.OfType<WoWGameObject>();

        /// <summary>
        /// Gets the current target of the player.
        /// </summary>
        static public WoWUnit CurrentTarget => Units.FirstOrDefault(u => Player.TargetGuid == u.Guid);

        /// <summary>
        /// Checks if the player is logged in.
        /// </summary>
        static public bool IsLoggedIn => Functions.GetPlayerGuid() > 0;

        /// <summary>
        /// Checks if the party members are grouped.
        /// </summary>
        static public bool IsGrouped => GetPartyMembers().Count() > 0;

        /// <summary>
        /// Gets the zone text. This property is weird and throws an exception right after entering the world,
        /// so we catch and ignore the exception to avoid console noise.
        /// </summary>
        static public string ZoneText
        {
            // this is weird and throws an exception right after entering world,
            // so we catch and ignore the exception to avoid console noise
            get
            {
                try
                {
                    var ptr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.ZoneTextPtr);
                    return MemoryManager.ReadString(ptr);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Gets the subzone text. This property is weird and throws an exception right after entering the world,
        /// so we catch and ignore the exception to avoid console noise.
        /// </summary>
        static public string SubZoneText
        {
            // this is weird and throws an exception right after entering world,
            // so we catch and ignore the exception to avoid console noise
            get
            {
                try
                {
                    var ptr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.SubZoneTextPtr);
                    return MemoryManager.ReadString(ptr);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Gets the minimap zone text. This property may throw an exception upon entering the world, but it is caught and ignored to avoid console noise.
        /// </summary>
        static public string MinimapZoneText
        {
            // this is weird and throws an exception right after entering world,
            // so we catch and ignore the exception to avoid console noise
            get
            {
                try
                {
                    var ptr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.MinimapZoneTextPtr);
                    return MemoryManager.ReadString(ptr);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Gets the map ID. This property may throw an exception upon entering the world, but it is caught and ignored to avoid console noise.
        /// </summary>
        static public uint MapId
        {
            // this is weird and throws an exception right after entering world,
            // so we catch and ignore the exception to avoid console noise
            get
            {
                try
                {
                    if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                    {
                        var objectManagerPtr = MemoryManager.ReadIntPtr((IntPtr)0x00B41414);
                        return MemoryManager.ReadUint(IntPtr.Add(objectManagerPtr, 0xCC));
                    }
                    else
                    {
                        return MemoryManager.ReadUint((IntPtr)MemoryAddresses.MapId);
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        static public string ServerName
        {
            // this is weird and throws an exception right after entering world,
            // so we catch and ignore the exception to avoid console noise
            get
            {
                try
                {
                    // not exactly sure how this works. seems to return a string like "Endless\WoW.exe" or "Karazhan\WoW.exe"
                    var fullName = MemoryManager.ReadString((IntPtr)MemoryAddresses.ServerName);
                    return fullName.Split('\\').First();
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Retrieves a collection of party members in the game.
        /// </summary>
        static public IEnumerable<WoWPlayer> GetPartyMembers()
        {
            var partyMembers = new List<WoWPlayer>();

            for (var i = 1; i < 5; i++)
            {
                var result = GetPartyMember(i);
                if (result != null)
                    partyMembers.Add(result);
            }

            return partyMembers;
        }

        /// <summary>
        /// Retrieves a party member based on the specified index.
        /// </summary>
        // index should be 1-4
        static WoWPlayer GetPartyMember(int index)
        {
            var result = Player?.LuaCallWithResults($"{{0}} = UnitName('party{index}')");

            if (result.Length > 0)
                return Players.FirstOrDefault(p => p.Name == result[0]);

            return null;
        }

        /// <summary>
        /// Gets a collection of WoWUnits that are aggressors.
        /// </summary>
        static public IEnumerable<WoWUnit> Aggressors =>
                    Units
                        .Where(u => u.Health > 0)
                        .Where(u =>
                            u.TargetGuid == Player?.Guid ||
                            u.TargetGuid == Pet?.Guid)
                        .Where(u =>
                            u.UnitReaction == UnitReaction.Hostile ||
                            u.UnitReaction == UnitReaction.Unfriendly ||
                            u.UnitReaction == UnitReaction.Neutral)
                        .Where(u => u.IsInCombat);

        /// <summary>
        /// Retrieves the rank of a talent based on the specified tab index and talent index.
        /// </summary>
        // https://vanilla-wow.fandom.com/wiki/API_GetTalentInfo
        // tab index is 1, 2 or 3
        // talentIndex is counter left to right, top to bottom, starting at 1
        static public sbyte GetTalentRank(int tabIndex, int talentIndex)
        {
            var results = Player.LuaCallWithResults($"{{0}}, {{1}}, {{2}}, {{3}}, {{4}} = GetTalentInfo({tabIndex},{talentIndex})");

            if (results.Length == 5)
                return Convert.ToSByte(results[4]);

            return -1;
        }

        /// <summary>
        /// Starts the enumeration of visible objects.
        /// </summary>
        static internal async void StartEnumeration()
        {
            while (true)
            {
                try
                {
                    EnumerateVisibleObjects();
                    await Task.Delay(500);
                }
                catch (Exception e)
                {
                    Logger.Log(e);
                }
            }
        }

        /// <summary>
        /// Enumerates the visible objects on the main thread.
        /// </summary>
        static void EnumerateVisibleObjects()
        {
            ThreadSynchronizer.RunOnMainThread(() =>
            {
                if (IsLoggedIn)
                {
                    playerGuid = Functions.GetPlayerGuid();
                    ObjectsBuffer.Clear();
                    Functions.EnumerateVisibleObjects(callbackPtr, 0);
                    Objects = new List<WoWObject>(ObjectsBuffer);

                    if (Player != null)
                    {
                        var petFound = false;

                        foreach (var unit in Units)
                        {
                            if (unit.SummonedByGuid == Player?.Guid)
                            {
                                Pet = new LocalPet(unit.Pointer, unit.Guid, unit.ObjectType);
                                petFound = true;
                            }

                            if (!petFound)
                                Pet = null;
                        }

                        Player.RefreshSpells();

                        UpdateProbe();
                    }
                }
            });
        }

        /// <summary>
        /// EnumerateVisibleObjects callback with swapped parameter order between Vanilla and other client versions.
        /// </summary>
        // EnumerateVisibleObjects callback has the parameter order swapped between Vanilla and other client versions.
        static int CallbackVanilla(int filter, ulong guid)
        {
            return CallbackInternal(guid, filter);
        }

        /// <summary>
        /// EnumerateVisibleObjects callback has the parameter order swapped between Vanilla and other client versions.
        /// </summary>
        // EnumerateVisibleObjects callback has the parameter order swapped between Vanilla and other client versions.
        static int CallbackNonVanilla(ulong guid, int filter)
        {
            return CallbackInternal(guid, filter);
        }

        /// <summary>
        /// Callback function that handles the internal logic for adding objects to the ObjectsBuffer based on the given guid and filter.
        /// </summary>
        static int CallbackInternal(ulong guid, int filter)
        {
            var pointer = Functions.GetObjectPtr(guid);
            var objectType = (ObjectType)MemoryManager.ReadInt(IntPtr.Add(pointer, OBJECT_TYPE_OFFSET));

            try
            {
                switch (objectType)
                {
                    case ObjectType.Container:
                        ObjectsBuffer.Add(new WoWContainer(pointer, guid, objectType));
                        break;
                    case ObjectType.Item:
                        ObjectsBuffer.Add(new WoWItem(pointer, guid, objectType));
                        break;
                    case ObjectType.Player:
                        if (guid == playerGuid)
                        {
                            var player = new LocalPlayer(pointer, guid, objectType);
                            Player = player;
                            ObjectsBuffer.Add(player);
                        }
                        else
                            ObjectsBuffer.Add(new WoWPlayer(pointer, guid, objectType));
                        break;
                    case ObjectType.GameObject:
                        ObjectsBuffer.Add(new WoWGameObject(pointer, guid, objectType));
                        break;
                    case ObjectType.Unit:
                        ObjectsBuffer.Add(new WoWUnit(pointer, guid, objectType));
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return 1;
        }

        /// <summary>
        /// Updates the probe with the current player and target information.
        /// If the player is in GM Island and the killswitch has not been triggered,
        /// engages the killswitch, stops player movement, and sends a killswitch alert.
        /// </summary>
        static void UpdateProbe()
        {
            if (Player != null)
            {
                // hit killswitch if player is in GM Island
                if (MinimapZoneText == "GM Island" && !KillswitchTriggered)
                {
                    Logger.Log("Killswitch Engaged");
                    Player.StopAllMovement();
                    probe.Killswitch();
                    DiscordClientWrapper.KillswitchAlert(Player.Name);
                    KillswitchTriggered = true;
                }

                probe.CurrentPosition = Player.Position.ToString();
                probe.CurrentZone = MinimapZoneText;

                var target = Units.FirstOrDefault(u => u.Guid == Player.TargetGuid);
                if (target != null)
                {
                    probe.TargetName = target.Name;
                    probe.TargetClass = Player.LuaCallWithResults($"{{0}} = UnitClass(\"target\")")[0];
                    probe.TargetCreatureType = target.CreatureType.ToString();
                    probe.TargetPosition = target.Position.ToString();
                    probe.TargetRange = Player.Position.DistanceTo(target.Position).ToString();
                    probe.TargetFactionId = target.FactionId.ToString();
                    probe.TargetIsCasting = target.IsCasting.ToString();
                    probe.TargetIsChanneling = target.IsChanneling.ToString();
                }

                probe.Callback();
            }
        }
    }
}
