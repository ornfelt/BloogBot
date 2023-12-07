using Binarysharp.Assemblers.Fasm;
using BloogBot.Game;
using BloogBot.Game.Cache;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// This namespace provides functionality for managing memory in a process.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// This class provides methods for managing memory in a process.
    /// </summary>
    /// <summary>
    /// This class provides methods for managing memory in a process.
    /// </summary>
    public static unsafe class MemoryManager
    {
        /// <summary>
        /// Specifies the access rights for a process.
        /// </summary>
        [Flags]
        enum ProcessAccessFlags
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            SYNCHRONIZE = 0x00100000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020
        }

        /// <summary>
        /// Imports the VirtualProtect function from the kernel32.dll library.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "VirtualProtect Function" as VP
        /// C -> VP : Calls VirtualProtect
        /// VP --> C : Returns bool result
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr address, int size, uint newProtect, out uint oldProtect);

        /// <summary>
        /// Opens an existing local process object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "OpenProcess Function" as O
        /// C -> O: Call OpenProcess(desiredAccess, inheritHandle, processId)
        /// O --> C: Returns IntPtr
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags desiredAccess, bool inheritHandle, int processId);

        /// <summary>
        /// Writes data to an area of memory in a specified process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> kernel32.dll: WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesWritten)
        /// kernel32.dll --> :Program: return bool
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
                            IntPtr hProcess,
                            IntPtr lpBaseAddress,
                            byte[] lpBuffer,
                            int dwSize,
                            ref int lpNumberOfBytesWritten);

        /// <summary>
        /// Specifies the protection options for a memory page.
        /// </summary>
        [Flags]
        public enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        /// <summary>
        /// The VirtualProtect function changes the protection on a region of committed pages in the virtual address space of the calling process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "VirtualProtect Function" as VP
        /// C -> VP: Call VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)
        /// VP --> C: Returns bool
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        /// <summary>
        /// The handle to the current process.
        /// </summary>
        static readonly IntPtr wowProcessHandle = Process.GetCurrentProcess().Handle;
        /// <summary>
        /// Represents a static instance of the FasmNet class.
        /// </summary>
        static readonly FasmNet fasm = new FasmNet();

        /// <summary>
        /// Reads a byte from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ReadByte -> IntPtr: Check if address is Zero
        /// IntPtr --> ReadByte: Return 0 if true
        /// ReadByte -> byte: Try to return byte at address
        /// byte --> ReadByte: Return byte if successful
        /// ReadByte -> Logger: Log Access Violation if unsuccessful
        /// Logger --> ReadByte: Return default byte
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static internal byte ReadByte(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return 0;

            try
            {
                return *(byte*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type Byte");
                return default;
            }
        }

        /// <summary>
        /// Reads an integer value from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate "ReadInt"
        /// "ReadInt" -> "ReadInt": Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        /// "ReadInt" --> "ReadInt": Return 0
        /// else address is not IntPtr.Zero
        /// "ReadInt" -> "ReadInt": Try to return value at address
        /// alt AccessViolationException is thrown
        /// "ReadInt" -> Logger: Log "Access Violation on " + address.ToString("X") + " with type Int"
        /// "ReadInt" --> "ReadInt": Return default
        /// end
        /// end
        /// deactivate "ReadInt"
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public int ReadInt(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return 0;

            try
            {
                return *(int*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type Int");
                return default;
            }
        }

        /// <summary>
        /// Reads an unsigned integer from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ReadUint Function" as R
        /// participant "Logger" as L
        /// 
        /// R -> R: Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        ///   R --> R: Return 0
        /// else address is not IntPtr.Zero
        ///   R -> R: Try to read uint from address
        ///   alt AccessViolationException is thrown
        ///     R -> L: Log "Access Violation on " + address.ToString("X") + " with type Uint"
        ///     R --> R: Return default
        ///   else No exception is thrown
        ///     R --> R: Return read uint
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public uint ReadUint(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return 0;

            try
            {
                return *(uint*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type Uint");
                return default;
            }
        }

        /// <summary>
        /// Reads an unsigned long integer from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ReadUlong -> address: Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        ///   ReadUlong --> ReadUlong: Return 0
        /// else address is not IntPtr.Zero
        ///   ReadUlong -> address: Try to read ulong from address
        ///   alt AccessViolationException is thrown
        ///     ReadUlong -> Logger: Log "Access Violation on " + address.ToString("X") + " with type Ulong"
        ///     ReadUlong --> ReadUlong: Return default
        ///   else no exception is thrown
        ///     ReadUlong --> ReadUlong: Return read ulong
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public ulong ReadUlong(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return 0;

            try
            {
                return *(ulong*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type Ulong");
                return default;
            }
        }

        /// <summary>
        /// Reads an IntPtr value from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ReadIntPtr Function" as R
        /// participant "Logger" as L
        /// 
        /// R -> R: Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        ///   R --> R: Return IntPtr.Zero
        /// else address is not IntPtr.Zero
        ///   R -> R: Try to return *(IntPtr*)address
        ///   alt AccessViolationException is thrown
        ///     R -> L: Log "Access Violation on " + address.ToString("X") + " with type IntPtr"
        ///     R --> R: Return default
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public IntPtr ReadIntPtr(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                return *(IntPtr*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type IntPtr");
                return default;
            }
        }

        /// <summary>
        /// Reads a float value from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate "ReadFloat"
        /// "ReadFloat" -> "ReadFloat": Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        /// "ReadFloat" --> "ReadFloat": Return 0
        /// else address is not IntPtr.Zero
        /// "ReadFloat" -> "ReadFloat": Try to return value at address
        /// alt AccessViolationException is thrown
        /// "ReadFloat" -> Logger: Log "Access Violation on " + address.ToString("X") + " with type Float"
        /// "ReadFloat" --> "ReadFloat": Return default
        /// end
        /// end
        /// deactivate "ReadFloat"
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public float ReadFloat(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return 0;

            try
            {
                return *(float*)address;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type Float");
                return default;
            }
        }

        /// <summary>
        /// Reads a string from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ReadString -> IntPtr: Check if address is Zero
        /// ReadString -> ReadBytes: Call ReadBytes with address and size
        /// ReadBytes --> ReadString: Return buffer
        /// ReadString -> Encoding: Get ASCII string from buffer
        /// Encoding --> ReadString: Return string
        /// ReadString -> String: Check for null character
        /// String --> ReadString: Return index
        /// ReadString -> String: Remove null character
        /// String --> ReadString: Return modified string
        /// ReadString -> Logger: Log Access Violation Exception
        /// Logger --> ReadString: Return default
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public string ReadString(IntPtr address, int size = 512)
        {
            if (address == IntPtr.Zero)
                return null;

            try
            {
                var buffer = ReadBytes(address, size);
                if (buffer == null)
                    return default;
                if (buffer.Length == 0)
                    return default;

                var ret = Encoding.ASCII.GetString(buffer);

                if (ret.IndexOf('\0') != -1)
                    ret = ret.Remove(ret.IndexOf('\0'));

                return ret;
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type string");
                return default;
            }
        }

        /// <summary>
        /// Reads a specified number of bytes from the memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ReadBytes:IntPtr, int --> ret:byte[]
        /// ret:byte[] --> ReadBytes:IntPtr, int
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public byte[] ReadBytes(IntPtr address, int count)
        {
            if (address == IntPtr.Zero)
                return null;

            try
            {
                var ret = new byte[count];
                var ptr = (byte*)address;

                for (var i = 0; i < count; i++)
                    ret[i] = ptr[i];

                return ret;
            }
            catch (NullReferenceException)
            {
                return default;
            }
            catch (AccessViolationException)
            {
                return default;
            }
        }

        /// <summary>
        /// Reads an item cache entry from the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ReadItemCacheEntry Function" as R
        /// participant "ItemCacheEntry Constructor" as I
        /// participant "Logger" as L
        /// 
        /// R -> R: Check if address is IntPtr.Zero
        /// alt address is IntPtr.Zero
        ///   R -> R: Return null
        /// else address is not IntPtr.Zero
        ///   R -> I: Create new ItemCacheEntry
        ///   alt AccessViolationException is thrown
        ///     I -> R: Throw AccessViolationException
        ///     R -> L: Log "Access Violation on " + address.ToString("X") + " with type ItemCacheEntry"
        ///     R -> R: Return default
        ///   else No exception is thrown
        ///     I -> R: Return new ItemCacheEntry
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        static public ItemCacheEntry ReadItemCacheEntry(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return null;

            try
            {
                return new ItemCacheEntry(address);
            }
            catch (AccessViolationException)
            {
                Logger.Log("Access Violation on " + address.ToString("X") + " with type ItemCacheEntry");
                return default;
            }
        }

        /// <summary>
        /// Writes a byte value to the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "WriteByte Function" as W
        /// participant "Marshal" as M
        /// C -> W: WriteByte(address, value)
        /// W -> M: StructureToPtr(value, address, false)
        /// \enduml
        /// </remarks>
        static internal void WriteByte(IntPtr address, byte value) => Marshal.StructureToPtr(value, address, false);

        /// <summary>
        /// Writes an integer value to the specified memory address.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "WriteInt Function" as W
        /// participant "Marshal" as M
        /// C -> W: WriteInt(address, value)
        /// W -> M: StructureToPtr(value, address, false)
        /// \enduml
        /// </remarks>
        static internal void WriteInt(IntPtr address, int value) => Marshal.StructureToPtr(value, address, false);

        /// <summary>
        /// Writes an array of bytes to a specified memory address, bypassing protection.
        /// </summary>
        // certain memory locations (Warden for example) are protected from modification.
        // we use OpenAccess with ProcessAccessFlags to remove the protection.
        // you can check whether memory is successfully being modified by setting a breakpoint
        // here and checking Debug -> Windows -> Disassembly.
        // if you have further issues, you may need to use VirtualProtect from the Win32 API.
        /// <remarks>
        /// \startuml
        /// participant "WriteBytes Function" as WriteBytes
        /// participant "OpenProcess Function" as OpenProcess
        /// participant "WriteProcessMemory Function" as WriteProcessMemory
        /// participant "VirtualProtect Function" as VirtualProtect
        /// 
        /// WriteBytes -> OpenProcess: access, false, Process.GetCurrentProcess().Id
        /// activate OpenProcess
        /// OpenProcess --> WriteBytes: process
        /// deactivate OpenProcess
        /// 
        /// WriteBytes -> WriteProcessMemory: process, address, bytes, bytes.Length, ref ret
        /// activate WriteProcessMemory
        /// WriteProcessMemory --> WriteBytes: ret
        /// deactivate WriteProcessMemory
        /// 
        /// WriteBytes -> VirtualProtect: address, bytes.Length, (uint)protection, out uint _
        /// activate VirtualProtect
        /// VirtualProtect --> WriteBytes: protection
        /// deactivate VirtualProtect
        /// \enduml
        /// </remarks>
        static internal void WriteBytes(IntPtr address, byte[] bytes)
        {
            if (address == IntPtr.Zero)
                return;

            var access = ProcessAccessFlags.PROCESS_CREATE_THREAD |
                         ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                         ProcessAccessFlags.PROCESS_SET_INFORMATION |
                         ProcessAccessFlags.PROCESS_TERMINATE |
                         ProcessAccessFlags.PROCESS_VM_OPERATION |
                         ProcessAccessFlags.PROCESS_VM_READ |
                         ProcessAccessFlags.PROCESS_VM_WRITE |
                         ProcessAccessFlags.SYNCHRONIZE;

            var process = OpenProcess(access, false, Process.GetCurrentProcess().Id);

            int ret = 0;
            WriteProcessMemory(process, address, bytes, bytes.Length, ref ret);

            var protection = Protection.PAGE_EXECUTE_READWRITE;
            // now set the memory to be executable
            VirtualProtect(address, bytes.Length, (uint)protection, out uint _);
        }

        /// <summary>
        /// Injects an assembly into memory and returns the starting address of the allocated area.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "InjectAssembly()" as IA
        /// participant "Fasm" as F
        /// participant "Logger" as L
        /// participant "Marshal" as M
        /// participant "Hack" as H
        /// participant "HackManager" as HM
        ///
        /// IA -> F: Clear()
        /// loop for each instruction
        /// IA -> F: AddLine(instruction)
        /// end
        /// IA -> F: Assemble()
        /// alt Exception occurs
        /// F -> IA: Throws FasmAssemblerException
        /// IA -> L: Log(ex)
        /// end
        /// IA -> M: AllocHGlobal(byteCode.Length)
        /// IA -> F: Clear()
        /// loop for each instruction
        /// IA -> F: AddLine(instruction)
        /// end
        /// IA -> F: Assemble(start)
        /// IA -> H: new Hack(hackName, start, byteCode)
        /// IA -> HM: AddHack(hack)
        /// IA --> : return start
        /// \enduml
        /// </remarks>
        static internal IntPtr InjectAssembly(string hackName, string[] instructions)
        {
            // first get the assembly as bytes for the allocated area before overwriting the memory
            fasm.Clear();
            fasm.AddLine("use32");
            foreach (var x in instructions)
                fasm.AddLine(x);

            var byteCode = new byte[0];
            try
            {
                byteCode = fasm.Assemble();
            }
            catch (FasmAssemblerException ex)
            {
                Logger.Log(ex);
            }

            var start = Marshal.AllocHGlobal(byteCode.Length);
            fasm.Clear();
            fasm.AddLine("use32");
            foreach (var x in instructions)
                fasm.AddLine(x);
            byteCode = fasm.Assemble(start);

            var hack = new Hack(hackName, start, byteCode);
            HackManager.AddHack(hack);

            return start;
        }

        /// <summary>
        /// Injects an assembly into the game process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "InjectAssembly Function" as IA
        /// participant "Fasm" as F
        /// participant "Hack" as H
        /// participant "HackManager" as HM
        /// 
        /// IA -> F: Clear()
        /// IA -> F: AddLine("use32")
        /// IA -> F: AddLine(instructions)
        /// IA -> F: Assemble(start)
        /// IA -> H: new Hack(hackName, start, byteCode)
        /// IA -> HM: AddHack(hack)
        /// \enduml
        /// </remarks>
        static internal void InjectAssembly(string hackName, uint ptr, string instructions)
        {
            fasm.Clear();
            fasm.AddLine("use32");
            fasm.AddLine(instructions);
            var start = new IntPtr(ptr);
            var byteCode = fasm.Assemble(start);

            var hack = new Hack(hackName, start, byteCode);
            HackManager.AddHack(hack);
        }
    }
}
