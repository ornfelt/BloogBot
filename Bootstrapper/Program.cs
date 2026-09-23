using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static Bootstrapper.WinImports;

namespace Bootstrapper
{
    class Program
    {
        static void Main()
        {
            var bootstrapperSettingsFilePath = ResolveFile("bootstrapperSettings.json");
            var bootstrapperSettings = JsonConvert.DeserializeObject<BootstrapperSettings>(File.ReadAllText(bootstrapperSettingsFilePath));

            var startupInfo = new STARTUPINFO();

            // Fail here, with the path in the message, rather than letting a bad PathToWoW turn into
            // 'Access is denied' from Process.Handle further down: CreateProcess would fail, leave
            // PROCESS_INFORMATION zeroed, and GetProcessById(0) would hand back the System Idle
            // process, which cannot be opened.
            if (!File.Exists(bootstrapperSettings.PathToWoW))
                throw new FileNotFoundException(
                    $"PathToWoW does not exist: {bootstrapperSettings.PathToWoW}" + Environment.NewLine +
                    $"Set it to your WoW.exe in {bootstrapperSettingsFilePath}");

            // run BloogBot.exe in a new process
            var processCreated = CreateProcess(
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

            // CreateProcess returns a bool that used to be discarded. Its Win32 error code is not
            // available - none of the DllImports sets SetLastError - but the path is the part
            // worth reporting anyway.
            if (!processCreated)
                throw new InvalidOperationException(
                    $"CreateProcess failed for {bootstrapperSettings.PathToWoW}. Check that it is a " +
                    "32-bit executable you have permission to run.");

            // this seems to help prevent timing issues
            Thread.Sleep(1000);

            // get a handle to the BloogBot process
            var processHandle = Process.GetProcessById((int)processInfo.dwProcessId).Handle;

            // resolve the file path to Loader.dll, probing the same locations, so an exe started
            // from a folder that does not hold the deployment still finds it
            var loaderPath = ResolveFile("Loader.dll");

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

        // bootstrapperSettings.json only lands next to Bootstrapper.exe when the BloogBot project
        // is built as well - it is BloogBot.csproj that copies it, not this project - so running or
        // debugging Bootstrapper on its own, or after cleaning the output folder, used to die with a
        // FileNotFoundException out of File.ReadAllText. Probe the obvious locations instead, and if
        // the file really is missing, say where we looked rather than just which path failed.
        static string ResolveFile(string fileName)
        {
            var roots = new List<string>();

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyDir))
                roots.Add(assemblyDir);

            var workingDir = Directory.GetCurrentDirectory();
            if (!roots.Contains(workingDir))
                roots.Add(workingDir);

            // For each root: the folder itself, then a Bot subfolder, then the same again for up to
            // four parents. That covers Bot\, Bot\Release\ (whose parent is Bot\), a project's own
            // bin\Debug\ inside the repo, and the repo root.
            var candidates = new List<string>();
            foreach (var root in roots)
            {
                var dir = new DirectoryInfo(root);
                for (var level = 0; level <= 4 && dir != null; level++, dir = dir.Parent)
                {
                    candidates.Add(Path.Combine(dir.FullName, fileName));
                    candidates.Add(Path.Combine(dir.FullName, "Bot", fileName));
                }
            }

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException(
                $"Could not find {fileName}. Looked in:{Environment.NewLine}  " +
                string.Join(Environment.NewLine + "  ", candidates));
        }
    }
}
