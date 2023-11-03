//
// Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//

/**
 * @file DetourAllocCustom.cpp
 * @brief Custom memory allocation functions for Detour.
 */
#include <stdlib.h>

#include "DetourAlloc.h"

 /**
  * @brief Default memory allocation function.
  *
  * @param size The size of memory to allocate.
  * @param hint Allocation hint (not used in this implementation).
  * @return A pointer to the allocated memory block.
  */
static void* dtAllocDefault(int size, dtAllocHint)
{
	return malloc(size);
}

/**
 * @brief Default memory deallocation function.
 *
 * @param ptr A pointer to the memory block to be deallocated.
 */
static void dtFreeDefault(void* ptr)
{
	free(ptr);
}

// Initialize custom memory allocation functions with defaults.
static dtAllocFunc* sAllocFunc = dtAllocDefault;
static dtFreeFunc* sFreeFunc = dtFreeDefault;

/**
 * @brief Set custom memory allocation and deallocation functions.
 *
 * This function allows you to set your own memory allocation and deallocation functions.
 * If custom functions are not provided (NULL), the default functions will be used.
 *
 * @param allocFunc Custom memory allocation function (or NULL for default).
 * @param freeFunc Custom memory deallocation function (or NULL for default).
 */
void dtAllocSetCustom(dtAllocFunc* allocFunc, dtFreeFunc* freeFunc)
{
	sAllocFunc = allocFunc ? allocFunc : dtAllocDefault;
	sFreeFunc = freeFunc ? freeFunc : dtFreeDefault;
}

/**
 * @brief Allocate memory with the specified size and allocation hint.
 *
 * This function is used to allocate memory for Detour data structures.
 *
 * @param size The size of memory to allocate.
 * @param hint Allocation hint (not used in this implementation).
 * @return A pointer to the allocated memory block.
 */
void* dtAlloc(int size, dtAllocHint hint)
{
	return sAllocFunc(size, hint);
}

/**
 * @brief Deallocate memory.
 *
 * This function is used to deallocate memory previously allocated with dtAlloc.
 * If the provided pointer is NULL, no action is taken.
 *
 * @param ptr A pointer to the memory block to be deallocated.
 */
void dtFree(void* ptr)
{
	if (ptr)
		sFreeFunc(ptr);
}
