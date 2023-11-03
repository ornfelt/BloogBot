#include "Navigation.h"
#include <windows.h>

extern "C"
{
	/**
	 * @brief Calculates a navigation path.
	 *
	 * This function calculates a navigation path between two points on the map.
	 *
	 * @param mapId The map ID.
	 * @param start The starting point (XYZ).
	 * @param end The destination point (XYZ).
	 * @param smoothPath Whether to calculate a smooth path.
	 * @param length A pointer to an integer to store the length of the path.
	 * @return A pointer to the calculated path (XYZ*).
	 */
	__declspec(dllexport) XYZ* CalculatePath(unsigned int mapId, XYZ start, XYZ end, bool smoothPath, int* length)
	{
		return Navigation::GetInstance()->CalculatePath(mapId, start, end, smoothPath, length);
	}

	/**
	 * @brief Frees a navigation path array.
	 *
	 * This function frees the memory associated with a navigation path array.
	 *
	 * @param pathArr A pointer to the navigation path array to free.
	 */
	__declspec(dllexport) void FreePathArr(XYZ* pathArr)
	{
		return Navigation::GetInstance()->FreePathArr(pathArr);
	}
};

/**
 * @brief DLL entry point.
 *
 * This function is the entry point for the DLL.
 *
 * @param hModule The handle to the DLL module.
 * @param ul_reason_for_call The reason for calling the DLL entry point.
 * @param lpReserved Reserved parameter (not used).
 * @return TRUE if initialization is successful, otherwise FALSE.
 */
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	Navigation* navigation = Navigation::GetInstance();
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		navigation->Initialize();
		break;

	case DLL_PROCESS_DETACH:
		navigation->Release();
		break;

	case DLL_THREAD_ATTACH:
		break;

	case DLL_THREAD_DETACH:
		break;
	}
	return TRUE;
}
