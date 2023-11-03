using BloogBot.Game;
using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// This class handles disabling Warden, a security feature in the game client.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// This class handles the disabling of Warden.
    /// </summary>
    /// <summary>
    /// This class handles the disabling of Warden.
    /// </summary>
    static class WardenDisabler
    {
        /// <summary>
        /// Represents the first 5 bytes of Warden's PageScan function.
        /// </summary>
        static readonly byte[] pageScanOriginalBytes = { 0x8B, 0x45, 0x08, 0x8A, 0x04 }; // first 5 bytes of Warden's PageScan function
        /// <summary>
        /// Represents the first 5 bytes of Warden's MemScan function.
        /// </summary>
        static readonly byte[] memScanOriginalBytes = { 0x56, 0x57, 0xFC, 0x8B, 0x54 }; // first 5 bytes of Warden's MemScan function

        /// <summary>
        /// Delegate for disabling Warden in the game. Different client versions may have different function signatures for this hook. The game may crash with an access violation if the wrong signature is used, possibly due to stack or register corruption.
        /// </summary>
        // different client versions have different function signatures for this hook. the game crashes with an access violation unless you
        // use the right signature here (likely due to stack or register corruption)
        delegate void DisableWardenVanillaDelegate(IntPtr _);
        /// <summary>
        /// Represents a static delegate for disabling the Warden Vanilla.
        /// </summary>
        static DisableWardenVanillaDelegate disableWardenVanillaDelegate;
        /// <summary>
        /// Represents a delegate that is used to disable the Warden TBC.
        /// </summary>
        delegate void DisableWardenTBCDelegate();
        /// <summary>
        /// Represents a delegate used to disable the Warden TBC.
        /// </summary>
        static DisableWardenTBCDelegate disableWardenTBCDelegate;
        /// <summary>
        /// Represents a delegate that is used to disable the Warden in World of Warcraft: Wrath of the Lich King.
        /// </summary>
        delegate void DisableWardenWotLKDelegate();
        /// <summary>
        /// Represents a delegate used to disable the Warden in World of Warcraft: Wrath of the Lich King.
        /// </summary>
        static DisableWardenWotLKDelegate disableWardenWotLKDelegate;

        /// <summary>
        /// Represents the pointer to the warden page scan function.
        /// </summary>
        static IntPtr wardenPageScanFunPtr = IntPtr.Zero;
        /// <summary>
        /// Represents the pointer to the memory scan function in the Warden.
        /// </summary>
        static IntPtr wardenMemScanFunPtr = IntPtr.Zero;

        /// <summary>
        /// Retrieves a handle to the specified module.
        /// </summary>
        // Module scan
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// Represents a module entry in a snapshot of the system's module list.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct MODULEENTRY32
        {
            /// <summary>
            /// Gets or sets the size of the dwSize field.
            /// </summary>
            internal uint dwSize;
            /// <summary>
            /// Gets or sets the module ID.
            /// </summary>
            internal uint th32ModuleID;
            /// <summary>
            /// Gets or sets the process ID.
            /// </summary>
            internal uint th32ProcessID;
            /// <summary>
            /// Represents the global count usage.
            /// </summary>
            internal uint GlblcntUsage;
            /// <summary>
            /// Gets or sets the usage count of the Proccnt.
            /// </summary>
            internal uint ProccntUsage;
            /// <summary>
            /// The base address of the module.
            /// </summary>
            private readonly IntPtr modBaseAddr;
            /// <summary>
            /// Gets or sets the size of the module base.
            /// </summary>
            internal uint modBaseSize;
            /// <summary>
            /// The handle to the loaded module.
            /// </summary>
            private readonly IntPtr hModule;
            /// <summary>
            /// The module size constant.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string szModule;
            /// <summary>
            /// Gets or sets the path of the executable file.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string szExePath;
        }

        /// <summary>
        /// Retrieves information about the first module in a specified snapshot of the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        /// <summary>
        /// Retrieves information about the next module in a snapshot of the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        /// <summary>
        /// Sets the last error code for the calling thread.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetLastError(uint dwErrCode);

        /// <summary>
        /// Delegate used by the Module32First function.
        /// </summary>
        static Module32FirstDelegate module32FirstDelegate;
        /// <summary>
        /// Represents the hook for the module32First function in the Detour class.
        /// </summary>
        static Detour module32FirstHook;

        /// <summary>
        /// Represents a delegate used to iterate through the modules in a process.
        /// </summary>
        static Module32NextDelegate module32NextDelegate;
        /// <summary>
        /// Represents a static Detour module32NextHook.
        /// </summary>
        static Detour module32NextHook;

        /// <summary>
        /// List of modules used by the program.
        /// </summary>
        static readonly IList<string> modules = new List<string>
        {
            "wow.exe",
            "ntdll.dll",
            "kernel32.dll",
            "kernelbase.dll",
            "advapi32.dll",
            "msvcrt.dll",
            "sechost.dll",
            "rpcrt4.dll",
            "sspicli.dll",
            "cryptbase.dll",
            "comctl32.dll",
            "gdi32.dll",
            "user32.dll",
            "lpk.dll",
            "usp10.dll",
            "shell32.dll",
            "shlwapi.dll",
            "wsock32.dll",
            "ws2_32.dll",
            "nsi.dll",
            "opengl32.dll",
            "glu32.dll",
            "ddraw.dll",
            "dciman32.dll",
            "setupapi.dll",
            "cfgmgr32.dll",
            "oleaut32.dll",
            "ole32.dll",
            "devobj.dll",
            "dwmapi.dll",
            "imm32.dll",
            "msctf.dll",
            "divxdecoder.dll",
            "winmm.dll",
            "fmod.dll",
            "msacm32.dll",
            "wininet.dll",
            "api-ms-win-downlevel-user32-l1-1-0.dll",
            "api-ms-win-downlevel-shlwapi-l1-1-0.dll",
            "api-ms-win-downlevel-version-l1-1-0.dll",
            "version.dll",
            "api-ms-win-downlevel-normaliz-l1-1-0.dll",
            "normaliz.dll",
            "iertutil.dll",
            "api-ms-win-downlevel-advapi32-l1-1-0.dll",
            "userenv.dll",
            "profapi.dll",
            "apphelp.dll",
            "acgenral.dll",
            "uxtheme.dll",
            "samcli.dll",
            "sfc.dll",
            "sfc_os.dll",
            "urlmon.dll",
            "api-ms-win-downlevel-ole32-l1-1-0.dll",
            "mpr.dll",
            "acspecfc.dll",
            "mscms.dll",
            "comdlg32.dll",
            "msi.dll",
            "ntmarta.dll",
            "wldap32.dll",
            "d3d9.dll",
            "d3d8thk.dll",
            "nvd3dum.dll",
            "psapi.dll",
            "powrprof.dll",
            "nvscpapi.dll",
            "dsound.dll",
            "clbcatq.dll",
            "mmdevapi.dll",
            "propsys.dll",
            "audioses.dll",
            "wdmaud.drv",
            "ksuser.dll",
            "avrt.dll",
            "msacm32.drv",
            "midimap.dll",
            "mswsock.dll",
            "wshtcpip.dll",
            "secur32.dll",
            "api-ms-win-downlevel-advapi32-l2-1-0.dll",
            "wship6.dll",
            "iphlpapi.dll",
            "winnsi.dll",
            "api-ms-win-downlevel-shlwapi-l2-1-0.dll",
            "dnsapi.dll",
            "rasadhlp.dll",
            "fwpuclnt.dll",
            "comctl32.dll",
            "nlaapi.dll",
            "napinsp.dll",
            "pnrpnsp.dll",
            "winrnr.dll"
        };

        /// <summary>
        /// Initializes the inline hook for detouring the WardenLoad call to ensure the detouring of Warden's scanning functions before they are called for the first time.
        /// The location of the 5 byte instruction used for the hook depends on the version of the WoW client being run.
        /// </summary>
        // we need a 5 byte instruction to use as a valid inline hook option.
        // it gets called when Warden is dynamically added to the WoW process,
        // so we hook here to detour the WardenLoad call to ensure
        // we can detour Warden's various scanning functions before they're
        // called for the first time. the location of this 5 byte instruction
        // depends on which version of the WoW client we're running.
        static internal void Initialize()
        {
            bool useWarden = false;
            if (useWarden)
            {
                string[] instructions = null;
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    disableWardenVanillaDelegate = DisableWardenVanilla;
                    var addrToDetour = Marshal.GetFunctionPointerForDelegate(disableWardenVanillaDelegate);

                    instructions = new[]
                    {
                    "MOV[0xCE8978], EAX",
                    "PUSHFD",
                    "PUSHAD",
                    "PUSH EAX",
                    $"CALL {(uint)addrToDetour}",
                    "POPAD",
                    "POPFD",
                    "JMP 0x006CA233"
                };
                }
                else if (ClientHelper.ClientVersion == ClientVersion.TBC)
                {
                    disableWardenTBCDelegate = DisableWardenTBC;
                    var addrToDetour = Marshal.GetFunctionPointerForDelegate(disableWardenTBCDelegate);

                    instructions = new[]
                    {
                    "PUSHFD",
                    "PUSHAD",
                    $"CALL {(uint)addrToDetour}",
                    "POPAD",
                    "POPFD",
                    "MOV ECX, 0x00E118EC",
                    "JMP 0x006D0C01"
                };
                }
                else if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    disableWardenWotLKDelegate = DisableWardenWotLK;
                    var addrToDetour = Marshal.GetFunctionPointerForDelegate(disableWardenWotLKDelegate);

                    instructions = new[]
                    {
                    "PUSHFD",
                    "PUSHAD",
                    $"CALL {(uint)addrToDetour}",
                    "POPAD",
                    "POPFD",
                    "MOV EAX, [EDI]",
                    "MOV ECX, [EAX+8]",
                    "JMP 0x008724C5"
                };
                }

                var wardenLoadDetour = MemoryManager.InjectAssembly("WardenLoadDetour", instructions);
                MemoryManager.InjectAssembly("WardenLoadHook", (uint)MemoryAddresses.WardenLoadHook, "JMP " + wardenLoadDetour);
                InitializeModuleScanHook();
            }
        }

        /// <summary>
        /// Disables the vanilla Warden.
        /// </summary>
        static void DisableWardenVanilla(IntPtr _)
        {
            DisableWardenInternal();
        }

        /// <summary>
        /// Disables the Warden for The Burning Crusade expansion.
        /// </summary>
        static void DisableWardenTBC()
        {
            DisableWardenInternal();
        }

        /// <summary>
        /// Disables the Warden for the Wrath of the Lich King expansion.
        /// </summary>
        static void DisableWardenWotLK()
        {
            DisableWardenInternal();
        }

        /// <summary>
        /// Disables the Warden internal functionality.
        /// </summary>
        static void DisableWardenInternal()
        {
            Console.WriteLine("[WARDEN] DisableWardenHook called.");
            var wardenPtr = MemoryManager.ReadIntPtr((IntPtr)MemoryAddresses.WardenBase);
            Console.WriteLine($"[WARDEN] WardenPtr = {wardenPtr} (0x{wardenPtr.ToString("X")})");
            if (wardenPtr != IntPtr.Zero)
            {
                if ((int)wardenPtr < 80000)
                {
                    // TODO: Warden disabling is not working reliably for WotLK. You can probably remove this warning once you have it more stable.
                    if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                    {
                        Console.WriteLine($"[WARDEN] Warden Module is at an unexpected memory address. Not hooking. PLAY AT YOUR OWN RISK!");
                    }
                }
                else
                {
                    var wardenBaseAddr = MemoryManager.ReadIntPtr(wardenPtr);
                    Console.WriteLine($"[WARDEN] DisableWardenHook found WardenBaseAddress = {wardenBaseAddr} (0x{wardenBaseAddr.ToString("X")})");
                    InitializeWardenPageScanHook(wardenBaseAddr);
                    InitializeWardenMemScanHook(wardenBaseAddr);
                }
            }
        }

        /// <summary>
        /// Initializes the WardenPageScanHook delegate.
        /// </summary>
        #region InitializeWardenPageScanHook
        delegate void WardenPageScanDelegate(IntPtr readBase, int readOffset, IntPtr writeTo);
        /// <summary>
        /// Represents a delegate for scanning a Warden page.
        /// </summary>
        static WardenPageScanDelegate wardenPageScanDelegate;

        /// <summary>
        /// Represents a static readonly byte array with a length of 4.
        /// </summary>
        static readonly byte[] seed = new byte[4];
        /// <summary>
        /// Represents a static readonly byte array with a length of 20.
        /// </summary>
        static readonly byte[] buffer = new byte[20];

        /// <summary>
        /// Initializes the Warden page scan hook by searching for the function signature in memory.
        /// If the function is found, it sets up the hook and prints a success message.
        /// If the function is not found or the hook fails, it prints a warning message.
        /// </summary>
        static void InitializeWardenPageScanHook(IntPtr wardenModuleStart)
        {
            IntPtr pageScanPtr = IntPtr.Zero;

            // in the WotLK client, the PageScan and MemScan functions seem to be loaded into memory at a random offset sometime after the WardenModule base address,
            // and about 6000 bytes later. I spent a bunch of time trying to figure out how to find the address of these functions deterministically, but failed.
            // so instead, we scan 5 bytes of memory 1 byte at a time until we find the function signature. and we start at 6000 and go down because occasionally
            // I've seen the same 5 bytes in memory more in more than one place, but through experimentation, it seems we always want the higher one.

            // TODO [12-3-2022]: this sorta works, but not consistently. need to come back and find a better way of doing this.
            for (var i = 0x10000; i > 0; i--)
            {
                var tempPageScanPtr = IntPtr.Add(wardenModuleStart, i);
                var currentBytes = MemoryManager.ReadBytes(tempPageScanPtr, 5);
                if (currentBytes != null && currentBytes.SequenceEqual(pageScanOriginalBytes))
                {
                    pageScanPtr = tempPageScanPtr;
                    break;
                }
            }

            if (pageScanPtr == IntPtr.Zero || pageScanPtr == wardenPageScanFunPtr)
            {
                // TODO: Warden disabling is not working reliably for WotLK. You can probably remove this warning once you have it more stable.
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    Console.WriteLine($"[WARDEN] Warden Module is at an unexpected memory address. Not hooking. PLAY AT YOUR OWN RISK!");
                }
                return;
            }

            Console.WriteLine("[WARDEN] PageScan module found in memory, continuing with hook...");

            wardenPageScanDelegate = WardenPageScanHook;
            var addrToDetour = Marshal.GetFunctionPointerForDelegate(wardenPageScanDelegate);

            var instructions = new[]
            {
                    "MOV EAX, [EBP+8]",
                    "PUSHFD",
                    "PUSHAD",
                    "MOV ECX, ESI",
                    "ADD ECX, EDI",
                    "ADD ECX, 0x1C",
                    "PUSH ECX",
                    "PUSH EDI",
                    "PUSH EAX",
                    $"CALL {(uint)addrToDetour}",
                    "POPAD",
                    "POPFD",
                    "INC EDI",
                    $"JMP {(uint)pageScanPtr + 0xB}"
                };

            var wardenPageScanDetourPtr = MemoryManager.InjectAssembly("WardenPageScanDetour", instructions);
            MemoryManager.InjectAssembly("WardenPageScanHook", (uint)pageScanPtr, "JMP 0x" + wardenPageScanDetourPtr.ToString("X"));

            wardenPageScanFunPtr = pageScanPtr;
            Console.WriteLine($"[WARDEN] PageScan Hooked! WardenModulePtr=0x{wardenModuleStart.ToString("X")} OriginalPageScanFunPtr=0x{pageScanPtr.ToString("X")} DetourFunPtr=0x{wardenPageScanDetourPtr.ToString("X")}");
        }

        /// <summary>
        /// Scans a page for hacks and performs necessary actions.
        /// </summary>
        static void WardenPageScanHook(IntPtr readBase, int readOffset, IntPtr writeTo)
        {
            // Logging this to the console lags the client like crazy.
            // Console.WriteLine($"[WARDEN PageScan] BaseAddr: {readBase.ToString("X")}, Offset: {readOffset}");

            var readByteFrom = readBase + readOffset;

            var hacksWithinRange = HackManager.Hacks.Where(h => h.IsWithinScanRange(readByteFrom, 1));

            foreach (var hack in hacksWithinRange)
                Console.WriteLine($"[WARDEN PageScan] Disabling {hack.Name} at {hack.Address.ToString("X")}");

            foreach (var hack in hacksWithinRange)
                HackManager.DisableHack(hack);

            MemoryManager.WriteByte(writeTo, MemoryManager.ReadByte(readBase + readOffset));

            foreach (var hack in hacksWithinRange)
                HackManager.EnableHack(hack);
        }
        /// <summary>
        /// Initializes the Warden memory scan hook.
        /// </summary>
        #endregion

        #region InitializeWardenMemScanHook
        delegate void WardenMemScanDelegate(IntPtr addr, int size, IntPtr bufferStart);
        /// <summary>
        /// Represents a delegate used for Warden memory scanning.
        /// </summary>
        static WardenMemScanDelegate wardenMemScanDelegate;

        /// <summary>
        /// Initializes the Warden memory scan hook by searching for the memory address of the Warden module and setting up the necessary detour.
        /// </summary>
        static void InitializeWardenMemScanHook(IntPtr wardenModuleStart)
        {
            IntPtr memScanPtr = IntPtr.Zero;
            for (var i = 0x10000; i > 0; i--)
            {
                var tempMemScanPtr = IntPtr.Add(wardenModuleStart, i);
                var currentBytes = MemoryManager.ReadBytes(tempMemScanPtr, 5);
                if (currentBytes != null && currentBytes.SequenceEqual(memScanOriginalBytes))
                {
                    memScanPtr = tempMemScanPtr;
                    break;
                }
            }

            if (memScanPtr == IntPtr.Zero || memScanPtr == wardenMemScanFunPtr)
            {
                // TODO: Warden disabling is not working reliably for WotLK. You can probably remove this warning once you have it more stable.
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    Console.WriteLine($"[WARDEN] Warden Module is at an unexpected memory address. Not hooking. PLAY AT YOUR OWN RISK!");
                }
                return;
            }

            Console.WriteLine("[WARDEN] MemScan module found in memory, continuing with hook...");

            wardenMemScanDelegate = WardenMemScanHook;
            var addrToDetour = Marshal.GetFunctionPointerForDelegate(wardenMemScanDelegate);

            var instructions = new[]
            {
                    "PUSH ESI",
                    "PUSH EDI",
                    "CLD",
                    "MOV EDX, [ESP+20]",
                    "MOV ESI, [ESP+16]",
                    "MOV EAX, [ESP+12]",
                    "MOV ECX, EDX",
                    "MOV EDI, EAX",
                    "PUSHFD",
                    "PUSHAD",
                    "PUSH EDI",
                    "PUSH ECX",
                    "PUSH ESI",
                    $"CALL 0x{((uint)addrToDetour).ToString("X")}",
                    "POPAD",
                    "POPFD",
                    "POP EDI",
                    "POP ESI",
                    $"JMP 0x{((uint) (memScanPtr + 0x24)).ToString("X")}",
                };

            var wardenMemScanDetourPtr = MemoryManager.InjectAssembly("WardenMemScanDetour", instructions);
            MemoryManager.InjectAssembly("WardenMemScanHook", (uint)memScanPtr, "JMP 0x" + wardenMemScanDetourPtr.ToString("X"));

            wardenMemScanFunPtr = memScanPtr;
            Console.WriteLine($"[WARDEN] MemScan Hooked! WardenModulePtr={wardenModuleStart.ToString("X")} OriginalMemScanFunPtr={memScanPtr.ToString("X")} DetourFunPtr={wardenMemScanDetourPtr.ToString("X")}");
        }

        /// <summary>
        /// Scans the memory for hacks within a specified range and performs necessary actions.
        /// </summary>
        static void WardenMemScanHook(IntPtr addr, int size, IntPtr bufferStart)
        {
            // todo: will size ever be 0?
            if (size != 0)
            {
                // Logging this to the console lags the client like crazy
                //Console.WriteLine($"[WARDEN MemoryScan] BaseAddr: {addr.ToString("X")}, Size: {size}");

                var hacksWithinRange = HackManager.Hacks
                    .Where(i => i.Address.ToInt32() <= IntPtr.Add(addr, size).ToInt32() && i.Address.ToInt32() >= addr.ToInt32());

                foreach (var hack in hacksWithinRange)
                    Console.WriteLine($"[WARDEN MemoryScan] Disabling {hack.Name} at {hack.Address.ToString("X")}");

                foreach (var hack in hacksWithinRange)
                    HackManager.DisableHack(hack);

                MemoryManager.WriteBytes(bufferStart, MemoryManager.ReadBytes(addr, size));

                foreach (var hack in hacksWithinRange)
                    HackManager.EnableHack(hack);
            }
        }
        /// <summary>
        /// Delegate for the Module32First function.
        /// </summary>
        #endregion

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate bool Module32FirstDelegate(IntPtr snapshot, ref MODULEENTRY32 module);

        /// <summary>
        /// Delegate for the Module32Next function, which retrieves information about the next module in a snapshot of the specified process.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate bool Module32NextDelegate(IntPtr snapshot, ref MODULEENTRY32 module);

        /// <summary>
        /// Initializes the module scan hook by registering delegates pointing to the functions we want to detour.
        /// </summary>
        static void InitializeModuleScanHook()
        {
            var handle = GetModuleHandle("kernel32.dll");
            var firstAddr = GetProcAddress(handle, "Module32First");
            var nextAddr = GetProcAddress(handle, "Module32Next");

            // Registering a delegate pointing to the function we want to detour
            module32FirstDelegate = Marshal.GetDelegateForFunctionPointer<Module32FirstDelegate>(firstAddr);
            module32FirstHook = new Detour(module32FirstDelegate, new Module32FirstDelegate(Module32FirstDetour), "Module32First");

            // Registering a delegate pointing to the function we want to detour
            module32NextDelegate = Marshal.GetDelegateForFunctionPointer<Module32NextDelegate>(nextAddr);
            module32NextHook = new Detour(module32NextDelegate, new Module32NextDelegate(Module32NextDetour), "Module32Next");

            Console.WriteLine("[WARDEN] ModuleScan Hooked!");
        }

        /// <summary>
        /// A static HashSet that stores protected items.
        /// </summary>
        static HashSet<string> protectedItems = new HashSet<string>();

        /// <summary>
        /// Detours the Module32First function to perform additional operations before calling the original function.
        /// </summary>
        static bool Module32FirstDetour(IntPtr snapshot, ref MODULEENTRY32 module)
        {
            Console.WriteLine("[WARDEN ModuleScan] Started");

            // TODO: try to read the Warden packet to see which Module they're scanning for
            //var ptr1 = MemoryManager.ReadIntPtr((IntPtr)WARDEN_PACKET_PTR);
            //var ptr2 = IntPtr.Add(ptr1, 0x634);
            //var ptr3 = MemoryManager.ReadIntPtr(ptr2);

            //seed[0] = MemoryManager.ReadByte(ptr3 + 3);
            //seed[1] = MemoryManager.ReadByte(ptr3 + 4);
            //seed[2] = MemoryManager.ReadByte(ptr3 + 5);
            //seed[3] = MemoryManager.ReadByte(ptr3 + 6);

            //for (var i = 7; i < 27; i++)
            //    buffer[i - 7] = MemoryManager.ReadByte(ptr3 + i);

            module32FirstHook.Remove();
            var ret = Module32First(snapshot, ref module);
            module32FirstHook.Apply();
            return ret;
        }

        /// <summary>
        /// Detours the Module32Next function to perform additional operations before and after calling the original function.
        /// </summary>
        static bool Module32NextDetour(IntPtr snapshot, ref MODULEENTRY32 module)
        {
            module32NextHook.Remove();
            var ret = Module32Next(snapshot, ref module);
            module32NextHook.Apply();

            var moduleName = module.szModule.ToUpper();
            var hmac = new HMACSHA1(seed);
            var dllBytes = Encoding.ASCII.GetBytes(moduleName);
            var dllHash = hmac.ComputeHash(dllBytes);
            var match = buffer.SequenceEqual(dllHash);

            if (match)
                Console.WriteLine($"[WARDEN ModuleScan] Scan detected for {moduleName}, detouring");

            while (!modules.Contains(module.szModule.ToLower()) && ret)
            {
                if (!protectedItems.Contains(module.szModule.ToLower()))
                    protectedItems.Add(module.szModule.ToLower());

                module32NextHook.Remove();
                ret = Module32Next(snapshot, ref module);
                module32NextHook.Apply();
            }
            if (!ret)
            {
                if (!modules.Contains(module.szModule.ToLower()))
                    module = new MODULEENTRY32 { dwSize = 548 };
                SetLastError(18);
            }
            return ret;
        }
    }
}
