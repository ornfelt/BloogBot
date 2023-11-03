#include "stdafx.h"

/**
 * @brief Entry point for a DLL application.
 *
 * @param hModule A handle to the DLL module.
 * @param ul_reason_for_call The reason code for the callback.
 * @param lpReserved Reserved.
 * @return TRUE on successful execution.
 */
BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}

	return TRUE;
}

extern "C"
{
	struct XYZXYZ { float X1; float Y1; float Z1; float X2; float Y2; float Z2; };
	struct Intersection { float X; float Y; float Z; float R; };
	struct XYZ { float X; float Y; float Z; };

	/**
	 * @brief Enumerates visible objects according to the given filter, calling back the provided function.
	 *
	 * @param callback A pointer to the callback function to be executed for each visible object.
	 * @param filter An integer representing the filter criteria.
	 * @param ptr A pointer to the function that will perform the enumeration.
	 */
	void __declspec(dllexport) __stdcall EnumerateVisibleObjects(unsigned int callback, int filter, unsigned int ptr)
	{
		typedef void __fastcall func(unsigned int callback, int filter);
		func* function = (func*)ptr;
		function(callback, filter);
	}

	/**
	 * @brief Executes Lua code on the game/client side.
	 *
	 * @param code A string containing the Lua code to be executed.
	 * @param ptr A pointer to the function that will execute the Lua code.
	 */
	void __declspec(dllexport) __stdcall LuaCall(char* code, unsigned int ptr)
	{
		typedef void __fastcall func(char* code, const char* unused);
		func* f = (func*)ptr;
		f(code, "Unused");
		return;
	}

	/**
	 * @brief Loots an item from a specific slot.
	 *
	 * @param slot The slot from which to loot the item.
	 * @param ptr A pointer to the function that will perform the looting.
	 */
	void __declspec(dllexport) __stdcall LootSlot(int slot, unsigned int ptr)
	{
		typedef void __fastcall func(unsigned int slot, int unused);
		func* f = (func*)ptr;
		f(slot, 0);
	}

	/**
	 * @brief Gets the text associated with a variable name.
	 *
	 * @param varName The name of the variable for which to get the text.
	 * @param parPtr A pointer to the function that will retrieve the text.
	 * @return The text associated with the variable name.
	 */
	unsigned int __declspec(dllexport) __stdcall GetText(char* varName, unsigned int parPtr)
	{
		typedef unsigned int __fastcall func(char* varName, unsigned int nonSense, int zero);
		func* f = (func*)parPtr;
		return f(varName, 0xFFFFFFFF, 0);
	}

	/**
	 * @brief Computes the intersection of two sets of points.
	 *
	 * @param points A pointer to the XYZXYZ structure containing points.
	 * @param distance A pointer to the distance information.
	 * @param intersection A pointer to the Intersection structure for the result.
	 * @param flags The intersection flags.
	 * @param ptr A pointer to the function that will perform the intersection.
	 * @return The result of the intersection.
	 */
	BYTE __declspec(dllexport) __stdcall Intersect(XYZXYZ* points, float* distance, Intersection* intersection, unsigned int flags, unsigned int ptr)
	{
		typedef BYTE __fastcall func(struct XYZXYZ* addrPoints, float* addrDistance, struct Intersection* addrIntersection, unsigned int flags);
		func* f = (func*)ptr;
		return f(points, distance, intersection, flags);
	}

	/**
	 * @brief Computes the intersection of two XYZ points.
	 *
	 * @param p1 The first XYZ point.
	 * @param p2 The second XYZ point.
	 * @param intersection A pointer to the XYZ structure for the result.
	 * @param distance A pointer to the distance information.
	 * @param flags The intersection flags.
	 * @param ptr A pointer to the function that will perform the intersection.
	 * @return True if there is an intersection, false otherwise.
	 */
	bool __declspec(dllexport) __stdcall Intersect2(XYZ* p1, XYZ* p2, XYZ* intersection, float* distance, unsigned int flags, unsigned int ptr)
	{
		typedef bool __fastcall func(XYZ* p1, XYZ* p2, int ignore, XYZ* intersection, float* distance, unsigned int flags);
		func* f = (func*)ptr;
		return f(p1, p2, 0, intersection, distance, flags);
	}

	/**
	 * @brief Sells an item to a vendor by its GUID.
	 *
	 * @param parCount The count of items to sell.
	 * @param parVendorGuid The GUID of the vendor.
	 * @param parItemGuid The GUID of the item to sell.
	 * @param parPtr A pointer to the function that will perform the selling.
	 */
	void __declspec(dllexport) __stdcall SellItemByGuid(unsigned int parCount, unsigned long long parVendorGuid, unsigned long long parItemGuid, unsigned int parPtr)
	{
		typedef void __fastcall func(unsigned int itemCount, unsigned int _zero, unsigned long long vendorGuid, unsigned long long itemGuid);
		func* f = (func*)parPtr;
		f(parCount, 0, parVendorGuid, parItemGuid);
	}

	/**
	 * @brief Buys an item from a vendor by its index and quantity.
	 *
	 * @param parItemIndex The index of the item to buy.
	 * @param parQuantity The quantity to buy.
	 * @param parVendorGuid The GUID of the vendor.
	 * @param parPtr A pointer to the function that will perform the buying.
	 */
	void __declspec(dllexport) __stdcall BuyVendorItem(int parItemIndex, int parQuantity, unsigned long long parVendorGuid, unsigned int parPtr)
	{
		typedef void __fastcall func(unsigned int itemIndex, unsigned int Quantity, unsigned long long vendorGuid, int _one);
		func* f = (func*)parPtr;
		f(parItemIndex, parQuantity, parVendorGuid, 5);
	}

	/**
	 * @brief Gets a pointer to an object based on type mask, GUID, line, and file information.
	 *
	 * @param parTypemask The type mask of the object.
	 * @param parObjectGuid The GUID of the object.
	 * @param parLine The line number.
	 * @param parFile The source file name.
	 * @param parPtr A pointer to the function that will retrieve the object pointer.
	 * @return A pointer to the object.
	 */
	unsigned int __declspec(dllexport) __stdcall GetObjectPtr(int parTypemask, unsigned long long parObjectGuid, int parLine, char* parFile, unsigned int parPtr)
	{
		typedef unsigned int __fastcall func(int typemask, unsigned long long objectGuid, int line, char* file);
		func* f = (func*)parPtr;
		return f(parTypemask, parObjectGuid, parLine, parFile);
	}
}