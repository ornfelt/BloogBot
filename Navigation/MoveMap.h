/**
* MaNGOS is a full featured server for World of Warcraft, supporting
* the following clients: 1.12.x, 2.4.3, 3.3.5a, 4.3.4a and 5.4.8
*
* Copyright (C) 2005-2015  MaNGOS project <http://getmangos.eu>
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*
* World of Warcraft, and all World of Warcraft or Warcraft art, images,
* and lore are copyrighted by Blizzard Entertainment, Inc.
*/

#ifndef MANGOS_H_MOVE_MAP
#define MANGOS_H_MOVE_MAP

#include <unordered_map>
#include <map>

#include "DetourAlloc.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"

#include "Utilities/UnorderedMapSet.h"

//  memory management

/**
 * @brief Custom memory allocation function for Detour.
 *
 * @param size The size of memory to allocate.
 * @param hint The allocation hint (not used).
 * @return A pointer to the allocated memory.
 */
inline void* dtCustomAlloc(int size, dtAllocHint /*hint*/)
{
	return (void*)new unsigned char[size];
}

/**
 * @brief Custom memory deallocation function for Detour.
 *
 * @param ptr A pointer to the memory to deallocate.
 */
inline void dtCustomFree(void* ptr)
{
	delete[](unsigned char*)ptr;
}

namespace MMAP
{
	typedef std::unordered_map<unsigned int, dtTileRef> MMapTileSet;
	typedef std::unordered_map<unsigned int, dtNavMeshQuery*> NavMeshQuerySet;

	/**
 * @brief Struct to hold data related to a map.
 */
	struct MMapData
	{
		MMapData(dtNavMesh* mesh) : navMesh(mesh) {}
		~MMapData()
		{
			for (NavMeshQuerySet::iterator i = navMeshQueries.begin(); i != navMeshQueries.end(); ++i)
			{
				dtFreeNavMeshQuery(i->second);
			}

			if (navMesh)
			{
				dtFreeNavMesh(navMesh);
			}
		}

		dtNavMesh* navMesh;

		// we have to use single dtNavMeshQuery for every instance, since those are not thread safe
		NavMeshQuerySet navMeshQueries;     // instanceId to query
		MMapTileSet mmapLoadedTiles;        // maps [map grid coords] to [dtTile]
	};

	typedef std::unordered_map<unsigned int, MMapData*> MMapDataSet;

	/**
	 * @brief Class to manage maps.
	 */
	class MMapManager
	{
	public:
		~MMapManager();

		std::map<unsigned int, bool> zoneMap = {};

		/**
		 * @brief Load a map.
		 *
		 * @param mapId The ID of the map to load.
		 * @param x The X coordinate.
		 * @param y The Y coordinate.
		 * @return True if the map was loaded successfully, false otherwise.
		 */
		bool loadMap(unsigned int mapId, int x, int y);

		/**
		 * @brief Get the navigation mesh query for a map instance.
		 *
		 * @param mapId The ID of the map.
		 * @param instanceId The instance ID.
		 * @return The navigation mesh query for the specified map instance.
		 */
		 // the returned [dtNavMeshQuery const*] is NOT threadsafe
		dtNavMeshQuery const* GetNavMeshQuery(unsigned int mapId, unsigned int instanceId);
		/**
		 * @brief Get the navigation mesh for a map.
		 *
		 * @param mapId The ID of the map.
		 * @return The navigation mesh for the specified map.
		 */
		dtNavMesh const* GetNavMesh(unsigned int mapId);

		/**
		 * @brief Get the count of loaded maps.
		 *
		 * @return The count of loaded maps.
		 */
		unsigned int getLoadedMapsCount() const { return loadedMMaps.size(); }
	private:
		bool loadMapData(unsigned int mapId);
		unsigned int packTileID(int x, int y);

		MMapDataSet loadedMMaps;
	};

	/**
	 * @brief Factory class to create or get an instance of MMapManager.
	 */
	class MMapFactory
	{
	public:
		static MMapManager* createOrGetMMapManager();
	};
}

#endif
