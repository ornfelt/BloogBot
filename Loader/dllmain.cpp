// Ported from BloogBot/Loader/dllmain.cpp (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
// credit to Zzuk: https://github.com/Zz9uk3/ZzukBot_V3/blob/master/Loader/Main.cpp

#define WIN32_LEAN_AND_MEAN

// The FOR_DOTNET_4 switch is gone with the API it selected. .NET 9 has no mscoree host and no
// ICLRMetaHostPolicy / ICLRRuntimeHost, so there is nothing left to pick between: the runtime is
// started through nethost / hostfxr instead.

// Typical windows shit
#include <Windows.h>
// _begingthreadex
#include <process.h>
// std::wstring
#include <string>
// CLR hosting API
// NETHOST_USE_AS_STATIC makes nethost.h declare get_hostfxr_path plainly instead of
// __declspec(dllimport); it is what libnethost.lib (static) expects. See the pragma below.
#define NETHOST_USE_AS_STATIC
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
// 'CorError.h' dropped with the ICLRRuntimeHost call it described - see the status switch below.
#include <iostream>

// No nethost library is linked. Linking the import library (nethost.lib) made Loader.dll
// depend on nethost.dll at load time, and LoadLibraryW does not search the loaded DLL's own
// directory for its dependencies - it searches the host process's directory (WoW.exe's),
// System32, the current directory and PATH. nethost.dll ships in Bot\\ next to Loader.dll, so
// it was not found, LoadLibraryW failed, and the injection died silently: no console, no
// message box, Bootstrapper.exe exiting 0. The .NET Framework original imported mscoree.dll,
// a system DLL that is always resolvable, so it never hit this.
// libnethost.lib (the static one) is not an option either: it is built /MT and this project is
// /MDd, and switching CRTs would undefine _DEBUG and silently drop the debugger wait above.
// So nethost.dll is loaded explicitly, by full path, next to this DLL - see LoadNetHost below.
typedef int (NETHOST_CALLTYPE *get_hostfxr_path_fn)(
	char_t *buffer, size_t *buffer_size, const struct get_hostfxr_parameters *parameters);

//#define LOAD_DLL_FILE_NAME L"DomainManager.dll"
//#define NAMESPACE_AND_CLASS L"DomainManager.EntryPoint"
//#define MAIN_METHOD L"Main"
//#define MAIN_METHOD_ARGS L"NONE"

// BloogBot is a class library on .NET 9, not a WinExe, and hostfxr starts the runtime from the
// runtimeconfig.json that BloogBot.csproj emits because of <EnableDynamicLoading>true</...>.
#define LOAD_DLL_FILE_NAME L"BloogBot.dll"
#define RUNTIME_CONFIG_FILE_NAME L"BloogBot.runtimeconfig.json"
#define NETHOST_FILE_NAME L"nethost.dll"
// Assembly-qualified now: load_assembly_and_get_function_pointer takes 'Namespace.Type, Assembly'.
#define NAMESPACE_AND_CLASS L"BloogBot.Loader, BloogBot"
#define MAIN_METHOD L"Load"
// MAIN_METHOD_ARGS is gone: component_entry_point_fn is 'int (void* arg, int32_t argSizeInBytes)',
// so there is no string argument to pass. ExecuteInDefaultAppDomain's L"NONE" had nowhere to go.

// Stored to avoid grabbing WoW's path. Instead we want the location
// of the actual DLL we're injecting.
HMODULE g_myDllModule = NULL;

HMODULE g_hostfxr = NULL;
hostfxr_close_fn g_closeFxr = NULL;
hostfxr_handle g_hostContext = NULL;

// Current running thread. Keep in mind; we can only use 1 instance of the CLR host.
// Lets make it useful shall we?
HANDLE g_hThread = NULL;

// Location of the DLL
wchar_t* dllLocation = NULL;
// Location of its runtimeconfig.json, which is what hostfxr is initialized from.
wchar_t* runtimeConfigLocation = NULL;
// Location of nethost.dll, which is loaded explicitly rather than imported. See the typedef above.
wchar_t* nethostLocation = NULL;

#define MB(s) MessageBoxW(NULL, s, NULL, MB_OK);

unsigned __stdcall ThreadMain(void* pParam)
{
	AllocConsole();
	freopen("CONOUT$", "w", stdout);


#ifdef USE_CUSTOM_CHANGES
	int skipDebug = 0;
	if (skipDebug)
		std::cout << std::string("Skipping attaching debugger...") << std::endl;
#if _DEBUG
	if (!skipDebug)
	{
		std::cout << std::string("Attach a debugger now to WoW.exe if you want to debug Loader.dll. Waiting 10 seconds...") << std::endl;

		HANDLE hEvent = CreateEvent(nullptr, TRUE, FALSE, L"MyDebugEvent");
		WaitForSingleObject(hEvent, 10000);  // Wait for 10 seconds
		bool isDebuggerAttached = IsDebuggerPresent() != FALSE;

		if (isDebuggerAttached)
		{
			std::cout << std::string("Debugger found.") << std::endl;
		}
		else
		{
			std::cout << std::string("Debugger not found.") << std::endl;
		}

		SetEvent(hEvent);
		CloseHandle(hEvent);
	}
#endif
#else
#if _DEBUG
	std::cout << std::string("Attach a debugger now to WoW.exe if you want to debug Loader.dll. Waiting 10 seconds...") << std::endl;

	HANDLE hEvent = CreateEvent(nullptr, TRUE, FALSE, L"MyDebugEvent");
	WaitForSingleObject(hEvent, 10000);  // Wait for 10 seconds
	bool isDebuggerAttached = IsDebuggerPresent() != FALSE;

	if (isDebuggerAttached)
	{
		std::cout << std::string("Debugger found.") << std::endl;
	}
	else
	{
		std::cout << std::string("Debugger not found.") << std::endl;
	}

	SetEvent(hEvent);
	CloseHandle(hEvent);
#endif
#endif


	// nethost resolves the hostfxr for this process's bitness - x86 here, since WoW.exe is 32-bit.
	// Replaces CLRCreateInstance(CLSID_CLRMetaHostPolicy, ...).
	HMODULE nethost = LoadLibraryW(nethostLocation);

	if (!nethost)
	{
		MB(L"Could not load nethost.dll -- it must sit next to Loader.dll.");
		return 1;
	}

	get_hostfxr_path_fn get_hostfxr_path_ptr =
		(get_hostfxr_path_fn)GetProcAddress(nethost, "get_hostfxr_path");

	if (!get_hostfxr_path_ptr)
	{
		MB(L"Could not resolve get_hostfxr_path in nethost.dll!");
		return 1;
	}

	wchar_t hostfxrPath[MAX_PATH];
	size_t hostfxrPathLength = MAX_PATH;
	int hr = get_hostfxr_path_ptr(hostfxrPath, &hostfxrPathLength, nullptr);

	if (FAILED(hr))
	{
		MB(L"Could not locate hostfxr -- is the x86 .NET runtime installed?");
		return 1;
	}

	g_hostfxr = LoadLibraryW(hostfxrPath);

	if (!g_hostfxr)
	{
		MB(L"Could not load hostfxr!");
		return 1;
	}

	hostfxr_initialize_for_runtime_config_fn initFxr =
		(hostfxr_initialize_for_runtime_config_fn)GetProcAddress(g_hostfxr, "hostfxr_initialize_for_runtime_config");
	hostfxr_get_runtime_delegate_fn getDelegate =
		(hostfxr_get_runtime_delegate_fn)GetProcAddress(g_hostfxr, "hostfxr_get_runtime_delegate");
	g_closeFxr = (hostfxr_close_fn)GetProcAddress(g_hostfxr, "hostfxr_close");

	if (!initFxr || !getDelegate || !g_closeFxr)
	{
		MB(L"Could not resolve the hostfxr entry points!");
		return 1;
	}

	// ICLRRuntimeInfo::BindAsLegacyV2Runtime used to sit here, for old .NET 3.5 mixed-mode DLLs.
	// It has no counterpart on .NET 9 - there is no legacy v2 binding policy and no mixed-mode
	// .NET 3.5 assembly left to bind for - so the call is simply gone.

	// Replaces ICLRMetaHostPolicy::GetRequestedRuntime + ICLRRuntimeHost::Start: the framework to
	// start is whatever BloogBot.runtimeconfig.json asks for.
	hr = initFxr(runtimeConfigLocation, nullptr, &g_hostContext);

	// Success_HostAlreadyInitialized (1) and Success_DifferentRuntimeProperties (2) are successes,
	// which is why this tests FAILED() rather than 'hr != 0'.
	if (FAILED(hr) || g_hostContext == NULL)
	{
		wchar_t buff[1024];
		wsprintf(buff, L"Could not initialize hostfxr -- hr = 0x%lx -- Is BloogBot.runtimeconfig.json present?", hr);
		MB(buff);

		return 1;
	}

	load_assembly_and_get_function_pointer_fn loadAssembly = NULL;
	hr = getDelegate(g_hostContext, hdt_load_assembly_and_get_function_pointer, (void**)&loadAssembly);

	if (FAILED(hr) || loadAssembly == NULL)
	{
		MB(L"Could not get the load_assembly_and_get_function_pointer delegate!");
		return 1;
	}

	component_entry_point_fn entry = NULL;
	// Replaces ICLRRuntimeHost::ExecuteInDefaultAppDomain. The null delegate type name selects the
	// default component_entry_point_fn signature, which is why BloogBot.Loader.Load is
	// 'public static int Load(IntPtr arg, int argSize)'.
	hr = loadAssembly(dllLocation, NAMESPACE_AND_CLASS, MAIN_METHOD, nullptr, nullptr, (void**)&entry);

	if (FAILED(hr) || entry == NULL)
	{
		MB(L"Failed to bind the managed entry point!");


		switch (hr)
		{
			// The HOST_E_* codes the ICLRRuntimeHost switch tested are Framework-only. These are
			// the hostfxr status codes that stand in for them, spelled numerically because
			// hostfxr ships no header for them.
		case 0x80008096:
			MB(L"CLR Not available");
			break;

		case 0x80008093:
			MB(L"Invalid runtimeconfig.json");
			break;

		case 0x80008089:
			MB(L"The CLR failed to initialize");
			break;

		case 0x80008098:
			MB(L"Buffer too small");
			break;

		case E_FAIL:
			MB(L"Unspecified catastrophic failure");
			break;

		default:
			char buff[128];
			sprintf(buff, "Result is: 0x%lx", hr);
			MessageBoxA(NULL, buff, "Info", 0);
			break;
		}

		return 1;
	}

	// Execute the Main func in the domain manager, this will block indefinitely.
	// (Hence why we're in our own thread!)
	entry(nullptr, 0);

	return 0;
}

void LoadClr()
{
	// POTENTIAL BUG FOUND: GetModuleFileNameW is given a fixed 255-wchar buffer. A module path
	//   longer than that is silently truncated - the call still returns non-zero, and the overflow
	//   is only visible through GetLastError() == ERROR_INSUFFICIENT_BUFFER - so the loader goes on
	//   to build a bad path to the managed assembly instead of returning.
	//   Original: BloogBot/Loader/dllmain.cpp:213
	//   Ported as-is - behavior matches .NET Framework BloogBot.
	wchar_t buffer[255];
	if (!GetModuleFileNameW(g_myDllModule, buffer, 255))
		return;

	std::wstring modulePath(buffer);

	// Get just the directory path.
	modulePath = modulePath.substr(0, modulePath.find_last_of('\\') + 1);
	std::wstring configPath(modulePath);
	std::wstring nethostPath(modulePath);
	modulePath = modulePath.append(LOAD_DLL_FILE_NAME);
	configPath = configPath.append(RUNTIME_CONFIG_FILE_NAME);
	nethostPath = nethostPath.append(NETHOST_FILE_NAME);

	// Copy the string, or we end up with junk data by the time we send it off
	// to our thread routine.
	dllLocation = new wchar_t[modulePath.length() + 1];
	wcscpy(dllLocation, modulePath.c_str());
	dllLocation[modulePath.length()] = '\0';

	runtimeConfigLocation = new wchar_t[configPath.length() + 1];
	wcscpy(runtimeConfigLocation, configPath.c_str());
	runtimeConfigLocation[configPath.length()] = '\0';

	nethostLocation = new wchar_t[nethostPath.length() + 1];
	wcscpy(nethostLocation, nethostPath.c_str());
	nethostLocation[nethostPath.length()] = '\0';

	g_hThread = (HANDLE)_beginthreadex(NULL, 0, ThreadMain, NULL, 0, NULL);
}

BOOL WINAPI DllMain(HMODULE hDll, DWORD dwReason, LPVOID lpReserved)
{
	g_myDllModule = hDll;
	if (dwReason == DLL_PROCESS_ATTACH)
	{
		LoadClr();
	}
	else if (dwReason == DLL_PROCESS_DETACH)
	{
		if (g_hostContext)
		{
			// There is no ICLRRuntimeHost::Stop on .NET 9 - the runtime cannot be unloaded from a
			// process - so closing the init context is all there is to do here. Nothing to
			// Release() either: hostfxr is a flat API, not a COM object.
			g_closeFxr(g_hostContext);
			g_hostContext = NULL;
		}

		// Yes yes, I know. I should be using _endthread(ex)
		// however, I can't. Since we don't want the thread killed until we exit.
		// POTENTIAL BUG FOUND: TerminateThread stops the managed thread wherever it happens to be,
		//   without unwinding it or letting it release anything it holds. Killed inside the CRT or
		//   while holding a lock, it leaves the heap or that lock permanently inconsistent, and
		//   DLL_PROCESS_DETACH runs under the loader lock, so a deadlock here hangs the process.
		//   Original: BloogBot/Loader/dllmain.cpp:265
		//   Ported as-is - behavior matches .NET Framework BloogBot.
		if (g_hThread)
		{
			TerminateThread(g_hThread, 0);
			CloseHandle(g_hThread);
		}
	}

	return TRUE;
}
