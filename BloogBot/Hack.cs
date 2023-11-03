using System;

/// <summary>
/// This class represents a hack that can modify the memory of a process.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a class that handles hacking operations.
    /// </summary>
    /// <summary>
    /// Represents a class that handles hacking operations.
    /// </summary>
    class Hack
    {
        /// <summary>
        /// Initializes a new instance of the Hack class with the specified name, address, and new bytes.
        /// </summary>
        internal Hack(string name, IntPtr address, byte[] newBytes)
        {
            Name = name;
            Address = address;
            NewBytes = newBytes;

            OriginalBytes = MemoryManager.ReadBytes(address, newBytes.Length);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the address of the object.
        /// </summary>
        internal IntPtr Address { get; }

        /// <summary>
        /// Gets the new bytes.
        /// </summary>
        internal byte[] NewBytes { get; }

        /// <summary>
        /// Gets the original bytes.
        /// </summary>
        internal byte[] OriginalBytes { get; }

        /// <summary>
        /// Determines if the specified memory range is within the scan range.
        /// </summary>
        internal bool IsWithinScanRange(IntPtr scanStartAddress, int size)
        {
            var scanStart = (int)scanStartAddress;
            var scanEnd = (int)IntPtr.Add(scanStartAddress, size);

            var hackStart = (int)Address;
            var hackEnd = (int)Address + NewBytes.Length;

            if (hackStart >= scanStart && hackStart < scanEnd)
                return true;

            if (hackEnd > scanStart && hackEnd <= scanEnd)
                return true;

            return false;
        }
    }
}
