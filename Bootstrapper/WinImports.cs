using System;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains the WinImports class which handles importing functions from the kernel32.dll library.
/// </summary>
namespace Bootstrapper
{
    /// <summary>
    /// This class contains imported functions from the Windows kernel32.dll library.
    /// </summary>
    /// <summary>
    /// This class contains imported functions from the Windows kernel32.dll library.
    /// </summary>
    static class WinImports
    {
        /// <summary>
        /// Creates a new process and its primary thread. The new process runs the specified executable file.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "CreateProcess Function" as CP
        /// C -> CP: Calls CreateProcess
        /// activate CP
        /// CP -> CP: Processes parameters
        /// CP --> C: Returns result (bool)
        /// deactivate CP
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        internal static extern bool CreateProcess(
                            string lpApplicationName,
                            string lpCommandLine,
                            IntPtr lpProcessAttributes,
                            IntPtr lpThreadAttributes,
                            bool bInheritHandles,
                            ProcessCreationFlag dwCreationFlags,
                            IntPtr lpEnvironment,
                            string lpCurrentDirectory,
                            ref STARTUPINFO lpStartupInfo,
                            out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// Retrieves a handle to the specified module.
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Allocates memory within the virtual address space of a specified process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller Function" as Caller
        /// participant "VirtualAllocEx Function" as VirtualAllocEx
        /// Caller -> VirtualAllocEx: Call VirtualAllocEx(hProcess, dwAddress, nSize, dwAllocationType, dwProtect)
        /// VirtualAllocEx --> Caller: Returns IntPtr
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr VirtualAllocEx(
                            IntPtr hProcess,
                            IntPtr dwAddress,
                            int nSize,
                            MemoryAllocationType dwAllocationType,
                            MemoryProtectionType dwProtect);

        /// <summary>
        /// Writes data to an area of memory in a specified process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "WriteProcessMemory" as W
        /// C -> W: Call WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, lpNumberOfBytesWritten)
        /// W --> C: Returns bool
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        internal static extern bool WriteProcessMemory(
                            IntPtr hProcess,
                            IntPtr lpBaseAddress,
                            byte[] lpBuffer,
                            int dwSize,
                            ref int lpNumberOfBytesWritten);

        /// <summary>
        /// Creates a thread that runs in the address space of another process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller Function" as Caller
        /// participant "CreateRemoteThread Function" as CRT
        /// 
        /// Caller -> CRT: Call CreateRemoteThread
        /// activate CRT
        /// CRT -> CRT: Initialize Remote Thread
        /// CRT --> Caller: Return Remote Thread Handle
        /// deactivate CRT
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateRemoteThread(
                            IntPtr hProcess,
                            IntPtr lpThreadAttribute,
                            IntPtr dwStackSize,
                            IntPtr lpStartAddress,
                            IntPtr lpParameter,
                            uint dwCreationFlags,
                            IntPtr lpThreadId);

        /// <summary>
        /// Frees a block of memory within a specified process.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "VirtualFreeEx Function" as V
        /// C -> V: Calls VirtualFreeEx
        /// note over V: Parameters: hProcess, dwAddress, nSize, dwFreeType
        /// V --> C: Returns bool
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        internal static extern bool VirtualFreeEx(
                            IntPtr hProcess,
                            IntPtr dwAddress,
                            int nSize,
                            MemoryFreeType dwFreeType);

        /// <summary>
        /// Represents the type of memory allocation.
        /// </summary>
        internal enum MemoryAllocationType
        {
            MEM_COMMIT = 0x1000
        }

        /// <summary>
        /// Represents the type of memory protection for a page in memory.
        /// </summary>
        internal enum MemoryProtectionType
        {
            PAGE_EXECUTE_READWRITE = 0x40
        }

        /// <summary>
        /// Represents the type of memory to be freed.
        /// </summary>
        internal enum MemoryFreeType
        {
            MEM_RELEASE = 0x8000
        }

        /// <summary>
        /// Represents a flag used during process creation to specify the default error mode.
        /// </summary>
        internal enum ProcessCreationFlag
        {
            CREATE_DEFAULT_ERROR_MODE = 0x04000000
        }

        /// <summary>
        /// Represents the startup information for a process.
        /// </summary>
        internal struct STARTUPINFO
        {
            /// <summary>
            /// The size, in bytes, of the data structure.
            /// </summary>
            public uint cb;
            /// <summary>
            /// Gets or sets the reserved string.
            /// </summary>
            public string lpReserved;
            /// <summary>
            /// Gets or sets the name of the desktop window.
            /// </summary>
            public string lpDesktop;
            /// <summary>
            /// Gets or sets the title of the lp.
            /// </summary>
            public string lpTitle;
            /// <summary>
            /// Gets or sets the X coordinate of the window position.
            /// </summary>
            public uint dwX;
            /// <summary>
            /// Represents the Y coordinate of a point in a 2D space.
            /// </summary>
            public uint dwY;
            /// <summary>
            /// Gets or sets the X size.
            /// </summary>
            public uint dwXSize;
            /// <summary>
            /// Gets or sets the Y size.
            /// </summary>
            public uint dwYSize;
            /// <summary>
            /// Gets or sets the number of characters in the X direction.
            /// </summary>
            public uint dwXCountChars;
            /// <summary>
            /// Gets or sets the number of characters in the vertical direction.
            /// </summary>
            public uint dwYCountChars;
            /// <summary>
            /// Gets or sets the fill attribute.
            /// </summary>
            public uint dwFillAttribute;
            /// <summary>
            /// Gets or sets the flags for the dwFlags property.
            /// </summary>
            public uint dwFlags;
            /// <summary>
            /// Gets or sets the window show state.
            /// </summary>
            public short wShowWindow;
            /// <summary>
            /// Gets or sets the reserved value 2.
            /// </summary>
            public short cbReserved2;
            /// <summary>
            /// Gets or sets the second reserved pointer.
            /// </summary>
            public IntPtr lpReserved2;
            /// <summary>
            /// Gets or sets the standard input handle for the process.
            /// </summary>
            public IntPtr hStdInput;
            /// <summary>
            /// Gets or sets the handle to the standard output device.
            /// </summary>
            public IntPtr hStdOutput;
            /// <summary>
            /// Gets or sets the standard error handle for the console.
            /// </summary>
            public IntPtr hStdError;
        }

        /// <summary>
        /// Contains information about a newly created process and its main thread.
        /// </summary>
        internal struct PROCESS_INFORMATION
        {
            /// <summary>
            /// Gets or sets the handle to the process.
            /// </summary>
            public IntPtr hProcess;
            /// <summary>
            /// Gets or sets the handle to the thread.
            /// </summary>
            public IntPtr hThread;
            /// <summary>
            /// Gets or sets the process ID.
            /// </summary>
            public uint dwProcessId;
            /// <summary>
            /// Gets or sets the thread identifier.
            /// </summary>
            public uint dwThreadId;
        }
    }
}
