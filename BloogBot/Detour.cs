using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains classes for handling detours in code execution.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a detour for hooking a method.
    /// </summary>
    /// <summary>
    /// Represents a detour for hooking a method.
    /// </summary>
    internal class Detour
    {
        /// <summary>
        /// The handle to the hook.
        /// </summary>
        readonly IntPtr hook;
        /// <summary>
        /// The delegate that is pinned here to prevent garbage collection.
        /// </summary>
        readonly Delegate hookDelegate; // pinned here to prevent GC
        /// <summary>
        /// Gets the target pointer.
        /// </summary>
        readonly IntPtr target;
        /// <summary>
        /// The target delegate is pinned here to prevent garbage collection.
        /// </summary>
        readonly Delegate targetDelegate; // pinned here to prevent GC
        /// <summary>
        /// Represents a read-only list of bytes.
        /// </summary>
        readonly List<byte> newBytes;
        /// <summary>
        /// Gets the original bytes.
        /// </summary>
        readonly List<byte> orginalBytes;

        /// <summary>
        /// Initializes a new instance of the Detour class.
        /// </summary>
        internal Detour(Delegate target, Delegate hook, string name)
        {
            Name = name;
            targetDelegate = target;
            this.target = Marshal.GetFunctionPointerForDelegate(target);
            hookDelegate = hook;
            this.hook = Marshal.GetFunctionPointerForDelegate(hook);

            //Store the orginal bytes
            orginalBytes = new List<byte>();
            orginalBytes.AddRange(MemoryManager.ReadBytes(this.target, 6));

            //Setup the detour bytes
            newBytes = new List<byte> { 0x68 };
            var tmp = BitConverter.GetBytes(this.hook.ToInt32());
            newBytes.AddRange(tmp);
            newBytes.Add(0xC3);

            var hack = new Hack(Name, this.target, newBytes.ToArray());
            HackManager.AddHack(hack);
        }

        /// <summary>
        /// Gets a value indicating whether the property is applied.
        /// </summary>
        public bool IsApplied { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Applies the new bytes to the target memory.
        /// </summary>
        public void Apply()
        {
            MemoryManager.WriteBytes(target, newBytes.ToArray());
        }

        /// <summary>
        /// Removes the target by writing the original bytes to the memory manager.
        /// </summary>
        public void Remove()
        {
            MemoryManager.WriteBytes(target, orginalBytes.ToArray());
        }
    }
}
