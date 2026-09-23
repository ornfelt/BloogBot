// credit to Zzuk: https://github.com/Zz9uk3/ZzukBot_V3/blob/master/Loader/Main.cpp

#define WIN32_LEAN_AND_MEAN

// Pick which CLR runtime we'll be using. 4.0 has different hosting APIs
// Comment this out if you're using .NET 3.5 or earlier.
#define FOR_DOTNET_4

// Typical windows shit
#include <Windows.h>
// _begingthreadex
#include <process.h>
// std::wstring
#include <string>
// std::vector, for the module-path buffer
#include <vector>
// CLR hosting API
#ifdef FOR_DOTNET_4
#include <metahost.h>
#else
#include <mscoree.h>
#endif
// CLR errors
#include "CorError.h"
#include <iostream>

// No rough configuration needed. :)
#pragma comment( lib, "mscoree" )

//#define LOAD_DLL_FILE_NAME L"DomainManager.dll"
//#define NAMESPACE_AND_CLASS L"DomainManager.EntryPoint"
//#define MAIN_METHOD L"Main"
//#define MAIN_METHOD_ARGS L"NONE"

#define LOAD_DLL_FILE_NAME L"BloogBot.exe"
#define NAMESPACE_AND_CLASS L"BloogBot.Loader"
#define MAIN_METHOD L"Load"
#define MAIN_METHOD_ARGS L"NONE"

// Stored to avoid grabbing WoW's path. Instead we want the location
// of the actual DLL we're injecting.
HMODULE g_myDllModule = NULL;

ICLRMetaHostPolicy* g_pMetaHost = NULL;
ICLRRuntimeInfo* g_pRuntimeInfo = NULL;
ICLRRuntimeHost* g_clrHost = NULL;

// Current running thread. Keep in mind; we can only use 1 instance of the CLR host.
// Lets make it useful shall we?
HANDLE g_hThread = NULL;

// Location of the DLL
wchar_t* dllLocation = NULL;

#define MB(s) MessageBoxW(NULL, s, NULL, MB_OK);

#if _DEBUG
// Waits out the debugger grace period, returning as soon as any of three things happens: the
// timeout elapses, the named MyDebugEvent is signalled from outside - which is what that event was
// always for - or Enter is pressed in the console ThreadMain just allocated. Without the last of
// those, every Debug injection cost a flat ten seconds even when nobody was going to attach.
static void WaitForDebuggerOrEnter(HANDLE hEvent, DWORD timeoutMs)
{
	const HANDLE input = GetStdHandle(STD_INPUT_HANDLE);

	DWORD consoleMode;
	if (input == NULL || input == INVALID_HANDLE_VALUE || !GetConsoleMode(input, &consoleMode))
	{
		// No console input to watch - wait exactly as before.
		WaitForSingleObject(hEvent, timeoutMs);
		return;
	}

	// Anything typed before we got here is not an answer to a prompt that had not been printed.
	FlushConsoleInputBuffer(input);

	const ULONGLONG deadline = GetTickCount64() + timeoutMs;
	HANDLE handles[2] = { hEvent, input };

	for (;;)
	{
		const ULONGLONG now = GetTickCount64();
		if (now >= deadline)
			return;

		const DWORD waited = WaitForMultipleObjects(2, handles, FALSE, (DWORD)(deadline - now));

		// Anything but 'the console has input' means we are done: MyDebugEvent fired, the timeout
		// expired, or the wait failed.
		if (waited != WAIT_OBJECT_0 + 1)
			return;

		// The handle also signals for key-up, mouse and focus records, so read them and look for a
		// real Enter press rather than trusting the wake-up on its own.
		INPUT_RECORD records[16];
		DWORD read = 0;
		if (!ReadConsoleInputW(input, records, ARRAYSIZE(records), &read))
			return;

		for (DWORD i = 0; i < read; i++)
		{
			if (records[i].EventType == KEY_EVENT &&
				records[i].Event.KeyEvent.bKeyDown &&
				records[i].Event.KeyEvent.wVirtualKeyCode == VK_RETURN)
			{
				std::cout << std::string("Enter pressed - skipping the wait.") << std::endl;
				return;
			}
		}
	}
}
#endif

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
		std::cout << std::string("Attach a debugger now to WoW.exe if you want to debug Loader.dll. Waiting 10 seconds... (press Enter to skip)") << std::endl;

		HANDLE hEvent = CreateEvent(nullptr, TRUE, FALSE, L"MyDebugEvent");
		WaitForDebuggerOrEnter(hEvent, 10000);  // up to 10 seconds; Enter skips it
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
	std::cout << std::string("Attach a debugger now to WoW.exe if you want to debug Loader.dll. Waiting 10 seconds... (press Enter to skip)") << std::endl;

	HANDLE hEvent = CreateEvent(nullptr, TRUE, FALSE, L"MyDebugEvent");
	WaitForDebuggerOrEnter(hEvent, 10000);  // up to 10 seconds; Enter skips it
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


	HRESULT hr = CLRCreateInstance(CLSID_CLRMetaHostPolicy, IID_ICLRMetaHostPolicy, (LPVOID*)&g_pMetaHost);

	if (FAILED(hr))
	{
		MB(L"Could not create instance of ICLRMetaHost");
		return 1;
	}

	//hr = g_pMetaHost->GetRuntime(L"v4.0.30128", IID_ICLRRuntimeInfo, (LPVOID*)&g_pRuntimeInfo);

	// Use this if your lazy. Just make sure to change the ICLRMetaHost
	// to an ICLRMetaHostPolicy instead.
	DWORD pcchVersion = 0;
	DWORD dwConfigFlags = 0;

	//wchar_t tmpbuff[1024];
	//wsprintf(tmpbuff, L"dllLocation: %s pcchVersion: %d dwConfigFlags: %d g_pMetaHost: 0x%lx", dllLocation, pcchVersion, dwConfigFlags, g_pMetaHost);
	//MB(tmpbuff);

	hr = g_pMetaHost->GetRequestedRuntime(METAHOST_POLICY_HIGHCOMPAT,
		dllLocation, NULL,
		NULL, &pcchVersion,
		NULL, NULL,
		&dwConfigFlags,
		IID_ICLRRuntimeInfo,
		(LPVOID*)&g_pRuntimeInfo);

	if (FAILED(hr))
	{
		if (hr == E_POINTER)
		{
			MB(L"Could not get an instance of ICLRRuntimeInfo -- E_POINTER");
		}
		else if (hr == E_INVALIDARG)
		{
			MB(L"Could not get an instance of ICLRRuntimeInfo -- E_INVALIDARG");
		}
		else
		{
			wchar_t buff[1024];
			wsprintf(buff, L"Could not get an instance of ICLRRuntimeInfo -- hr = 0x%lx -- Is DomainManager.dll present?", hr);
			MB(buff);
		}

		return 1;
	}

	// We need this if we have old .NET 3.5 mixed-mode DLLs
	hr = g_pRuntimeInfo->BindAsLegacyV2Runtime();

	if (FAILED(hr))
	{
		MB(L"Failed to bind as legacy v2 runtime! (.NET 3.5 Mixed-Mode Support)");
		return 1;
	}

	hr = g_pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&g_clrHost);

	if (FAILED(hr))
	{
		MB(L"Could not get an instance of ICLRRuntimeHost!");
		return 1;
	}

	hr = g_clrHost->Start();

	if (FAILED(hr))
	{
		MB(L"Failed to start the CLR!");
		return 1;
	}

	DWORD dwRet = 0;
	// Execute the Main func in the domain manager, this will block indefinitely.
	// (Hence why we're in our own thread!)
	hr = g_clrHost->ExecuteInDefaultAppDomain(dllLocation, NAMESPACE_AND_CLASS, MAIN_METHOD, MAIN_METHOD_ARGS, &dwRet);

	if (FAILED(hr))
	{
		MB(L"Failed to execute in the default app domain!");


		switch (hr)
		{
		case HOST_E_CLRNOTAVAILABLE:
			MB(L"CLR Not available");
			break;

		case HOST_E_TIMEOUT:
			MB(L"Call timed out");
			break;

		case HOST_E_NOT_OWNER:
			MB(L"Caller does not own lock");
			break;

		case HOST_E_ABANDONED:
			MB(L"An event was canceled while a blocked thread or fiber was waiting on it");
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

	return 0;
}

void LoadClr()
{
	// GetModuleFileNameW truncates silently when the buffer is too small: it copies as much as
	// fits, returns the buffer size rather than failing, and reports the overflow only through
	// ERROR_INSUFFICIENT_BUFFER. The fixed wchar_t[255] this used to use therefore turned a long
	// install path into a quietly wrong path to the managed assembly. Grow until it fits.
	std::wstring modulePath;
	for (DWORD capacity = MAX_PATH; capacity <= 65536; capacity *= 2)
	{
		std::vector<wchar_t> buffer(capacity);
		SetLastError(ERROR_SUCCESS);
		const DWORD copied = GetModuleFileNameW(g_myDllModule, buffer.data(), capacity);

		if (copied == 0)
			return;

		if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
		{
			modulePath.assign(buffer.data(), copied);
			break;
		}
	}

	if (modulePath.empty())
		return;

	// Get just the directory path.
	modulePath = modulePath.substr(0, modulePath.find_last_of('\\') + 1);
	modulePath = modulePath.append(LOAD_DLL_FILE_NAME);

	// Copy the string, or we end up with junk data by the time we send it off
	// to our thread routine.
	dllLocation = new wchar_t[modulePath.length() + 1];
	wcscpy(dllLocation, modulePath.c_str());
	dllLocation[modulePath.length()] = '\0';

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
		if (g_clrHost)
		{
			// We eventually 'die' so we make sure we stop the CLR.
			g_clrHost->Stop();
			// And release it.
			g_clrHost->Release();
		}

		// The thread is deliberately not terminated here. On process exit the loader has already
		// stopped every other thread before this notification arrives, so there is nothing left
		// to kill; and on a FreeLibrary detach TerminateThread would stop the managed thread
		// wherever it happened to be, without unwinding it or letting it release a lock or the
		// CRT heap - and DLL_PROCESS_DETACH runs under the loader lock, so that is a good way to
		// hang the process. Closing the handle is all that is needed.
		if (g_hThread)
		{
			CloseHandle(g_hThread);
			g_hThread = NULL;
		}
	}

	return TRUE;
}