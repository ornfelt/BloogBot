using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static Bootstrapper.WinImports;

/// <summary>
/// This namespace contains the main entry point for the bootstrapper program.
/// </summary>
namespace Bootstrapper
{
    /// <summary>
    /// The main entry point for the application. It executes the BloogBot application by creating a new process and loading the necessary DLL files.
    /// </summary>
    /// <summary>
    /// The main entry point for the application. It executes the BloogBot application by creating a new process and loading the necessary DLL files.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Executes the BloogBot application by creating a new process and loading the necessary DLL files.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Main -> Path : GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        /// Main -> Path : Combine(currentFolder, "bootstrapperSettings.json")
        /// Main -> JsonConvert : DeserializeObject<BootstrapperSettings>(File.ReadAllText(bootstrapperSettingsFilePath))
        /// Main -> CreateProcess : CreateProcess(bootstrapperSettings.PathToWoW, null, IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlag.CREATE_DEFAULT_ERROR_MODE, IntPtr.Zero, null, ref startupInfo, out PROCESS_INFORMATION processInfo)
        /// Main -> Thread : Sleep(1000)
        /// Main -> Process : GetProcessById((int)processInfo.dwProcessId).Handle
        /// Main -> Path : Combine(currentFolder, "Loader.dll")
        /// Main -> VirtualAllocEx : VirtualAllocEx(processHandle, (IntPtr)0, loaderPath.Length, MemoryAllocationType.MEM_COMMIT, MemoryProtectionType.PAGE_EXECUTE_READWRITE)
        /// Main -> Thread : Sleep(500)
        /// Main -> Marshal : GetLastWin32Error()
        /// Main -> WriteProcessMemory : WriteProcessMemory(processHandle, loaderPathPtr, bytes, bytes.Length, ref bytesWritten)
        /// Main -> Thread : Sleep(1000)
        /// Main -> Marshal : GetLastWin32Error()
        /// Main -> GetProcAddress : GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW")
        /// Main -> Thread : Sleep(1000)
        /// Main -> Marshal : GetLastWin32Error()
        /// Main -> CreateRemoteThread : CreateRemoteThread(processHandle, (IntPtr)null, (IntPtr)0, loaderDllPointer, loaderPathPtr, 0, (IntPtr)null)
        /// Main -> Thread : Sleep(1000)
        /// Main -> Marshal : GetLastWin32Error()
        /// Main -> VirtualFreeEx : VirtualFreeEx(processHandle, loaderPathPtr, 0, MemoryFreeType.MEM_RELEASE)
        /// \enduml
        /// </remarks>
        static void Main()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bootstrapperSettingsFilePath = Path.Combine(currentFolder, "bootstrapperSettings.json");
            var bootstrapperSettings = JsonConvert.DeserializeObject<BootstrapperSettings>(File.ReadAllText(bootstrapperSettingsFilePath));

            var startupInfo = new STARTUPINFO();

            // run BloogBot.exe in a new process
            CreateProcess(
                bootstrapperSettings.PathToWoW,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ProcessCreationFlag.CREATE_DEFAULT_ERROR_MODE,
                IntPtr.Zero,
                null,
                ref startupInfo,
                out PROCESS_INFORMATION processInfo);

            // this seems to help prevent timing issues
            Thread.Sleep(1000);

            // get a handle to the BloogBot process
            var processHandle = Process.GetProcessById((int)processInfo.dwProcessId).Handle;

            // resolve the file path to Loader.dll relative to our current working directory
            var loaderPath = Path.Combine(currentFolder, "Loader.dll");

            // allocate enough memory to hold the full file path to Loader.dll within the BloogBot process
            var loaderPathPtr = VirtualAllocEx(
                processHandle,
                (IntPtr)0,
                loaderPath.Length,
                MemoryAllocationType.MEM_COMMIT,
                MemoryProtectionType.PAGE_EXECUTE_READWRITE);

            // this seems to help prevent timing issues
            Thread.Sleep(500);

            int error = Marshal.GetLastWin32Error();
            if (error > 0)
                throw new InvalidOperationException($"Failed to allocate memory for Loader.dll, error code: {error}");

            // write the file path to Loader.dll to the EoE process's memory
            var bytes = Encoding.Unicode.GetBytes(loaderPath);
            var bytesWritten = 0; // throw away
            WriteProcessMemory(processHandle, loaderPathPtr, bytes, bytes.Length, ref bytesWritten);

            // this seems to help prevent timing issues
            Thread.Sleep(1000);

            error = Marshal.GetLastWin32Error();
            if (error > 0 || bytesWritten == 0)
                throw new InvalidOperationException($"Failed to write Loader.dll into the WoW.exe process, error code: {error}");

            // search current process's for the memory address of the LoadLibraryW function within the kernel32.dll module
            var loaderDllPointer = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");

            // this seems to help prevent timing issues
            Thread.Sleep(1000);

            error = Marshal.GetLastWin32Error();
            if (error > 0)
                throw new InvalidOperationException($"Failed to get memory address to Loader.dll in the WoW.exe process, error code: {error}");

            // create a new thread with the execution starting at the LoadLibraryW function, 
            // with the path to our Loader.dll passed as a parameter
            CreateRemoteThread(processHandle, (IntPtr)null, (IntPtr)0, loaderDllPointer, loaderPathPtr, 0, (IntPtr)null);

            // this seems to help prevent timing issues
            Thread.Sleep(1000);

            error = Marshal.GetLastWin32Error();
            if (error > 0)
                throw new InvalidOperationException($"Failed to create remote thread to start execution of Loader.dll in the WoW.exe process, error code: {error}");

            // free the memory that was allocated by VirtualAllocEx
            VirtualFreeEx(processHandle, loaderPathPtr, 0, MemoryFreeType.MEM_RELEASE);
        }
    }
}
