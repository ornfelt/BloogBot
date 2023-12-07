﻿using System.Collections.Generic;

/// <summary>
/// Disables a hack by restoring the original bytes at the hack's address.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Manages a collection of hacks and enables them by writing new bytes to specified memory addresses.
    /// </summary>
    /// <summary>
    /// Manages a collection of hacks and enables them by writing new bytes to specified memory addresses.
    /// </summary>
    static class HackManager
    {
        /// <summary>
        /// Gets the list of hacks.
        /// </summary>
        static internal IList<Hack> Hacks { get; } = new List<Hack>();

        /// <summary>
        /// Adds a hack to the list of hacks and enables it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddHack -> Hacks: Add(hack)
        /// AddHack -> : EnableHack(hack)
        /// \enduml
        /// </remarks>
        static internal void AddHack(Hack hack)
        {
            Hacks.Add(hack);
            EnableHack(hack);
        }

        /// <summary>
        /// Enables a hack by writing new bytes to the specified address in memory.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Hack" as H
        /// participant "MemoryManager" as M
        /// H -> M: WriteBytes(hack.Address, hack.NewBytes)
        /// \enduml
        /// </remarks>
        static internal void EnableHack(Hack hack) => MemoryManager.WriteBytes(hack.Address, hack.NewBytes);

        /// <summary>
        /// Disables a hack by writing the original bytes to the hack's address in memory.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "DisableHack(Hack hack)" as A
        /// participant "MemoryManager" as B
        /// A -> B: WriteBytes(hack.Address, hack.OriginalBytes)
        /// \enduml
        /// </remarks>
        static internal void DisableHack(Hack hack) => MemoryManager.WriteBytes(hack.Address, hack.OriginalBytes);
    }
}
