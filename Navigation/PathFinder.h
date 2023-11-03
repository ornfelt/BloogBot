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

#ifndef MANGOS_PATH_FINDER_H
#define MANGOS_PATH_FINDER_H

#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"

#include "MoveMapSharedDefines.h"
#include "G3D/Vector3.h"

namespace Movement
{
	using G3D::Vector2;
	using G3D::Vector3;
	using G3D::Vector4;
	typedef std::vector<Vector3> PointsArray;
}

using Movement::Vector3;
using Movement::PointsArray;

/**
 * @brief Maximum length of a path.
 */
 // 74*4.0f=296y  number_of_points*interval = max_path_len
 // this is way more than actual evade range
 // I think we can safely cut those down even more
#define MAX_PATH_LENGTH         740//74
/**
 * @brief Maximum length of a point-based path.
 */
#define MAX_POINT_PATH_LENGTH   740//74

 /**
  * @brief Size of each step in smoothing the path.
  */
#define SMOOTH_PATH_STEP_SIZE   4.0f
  /**
   * @brief Slope factor used in smoothing the path.
   */
#define SMOOTH_PATH_SLOP        0.3f

   /**
	* @brief Size of a vertex.
	*/
#define VERTEX_SIZE       3
	/**
	 * @brief Invalid polygon reference value.
	 */
#define INVALID_POLYREF   0

	 /**
	  * @brief Flags for different types of map liquids.
	  */
	  // defined in DBC and left shifted for flag usage
#define MAP_LIQUID_TYPE_NO_WATER    0x00
#define MAP_LIQUID_TYPE_MAGMA       0x01
#define MAP_LIQUID_TYPE_OCEAN       0x02
#define MAP_LIQUID_TYPE_SLIME       0x04
#define MAP_LIQUID_TYPE_WATER       0x08

/**
 * @brief Combination of all map liquids flags.
 */
#define MAP_ALL_LIQUIDS   (MAP_LIQUID_TYPE_WATER | MAP_LIQUID_TYPE_MAGMA | MAP_LIQUID_TYPE_OCEAN | MAP_LIQUID_TYPE_SLIME)

 /**
  * @brief Flags for additional map liquids.
  */
#define MAP_LIQUID_TYPE_DARK_WATER  0x10
#define MAP_LIQUID_TYPE_WMO_WATER   0x20

  /**
   * @brief Structure to hold liquid data for grid maps.
   */
struct GridMapLiquidData
{
	unsigned int type_flags;
	unsigned int entry;
	float level;
	float depth_level;
};

/**
 * @brief Enumeration representing different path types.
 */
enum PathType
{
	PATHFIND_BLANK = 0x0000,   // path not built yet
	PATHFIND_NORMAL = 0x0001,   // normal path
	PATHFIND_SHORTCUT = 0x0002,   // travel through obstacles, terrain, air, etc (old behavior)
	PATHFIND_INCOMPLETE = 0x0004,   // we have partial path to follow - getting closer to target
	PATHFIND_NOPATH = 0x0008,   // no valid path at all or error in generating one
	PATHFIND_NOT_USING_PATH = 0x0010    // used when we are either flying/swiming or on map w/o mmaps
};

/**
 * @brief Class for pathfinding operations using Detour navigation mesh.
 */
class PathFinder
{
public:
	/**
	 * @brief Constructor for the PathFinder class.
	 *
	 * @param mapId The ID of the map.
	 * @param instanceId The ID of the instance.
	 */
	PathFinder(unsigned int mapId, unsigned int instanceId);
	/**
	 * @brief Destructor for the PathFinder class.
	 */
	~PathFinder();

	/**
	 * @brief Calculate the path from owner to given destination
	 *
	 * @param originX The X coordinate of the current position.
	 * @param originY The Y coordinate of the current position.
	 * @param originZ The Z coordinate of the current position.
	 * @param destX The X coordinate of the destination.
	 * @param destY The Y coordinate of the destination.
	 * @param destZ The Z coordinate of the destination.
	 * @param forceDest Whether to force arrival at the destination.
	 * @param isSwimming Whether the character is swimming.
	 * @return True if a new path was calculated, false otherwise.
	 */
	bool calculate(float originX, float originY, float originZ, float destX, float destY, float destZ, bool forceDest = false, bool isSwimming = false);

	/**
	 * @brief Set whether to use straight paths.
	 *
	 * @param useStraightPath True to use straight paths, false otherwise.
	 */
	void setUseStrightPath(bool useStraightPath) { m_useStraightPath = useStraightPath; };
	/**
	 * @brief Set the path length limit.
	 *
	 * @param distance The maximum distance for the path length limit.
	 */
	void setPathLengthLimit(float distance) { m_pointPathLimit = std::min<unsigned int>(unsigned int(distance / SMOOTH_PATH_STEP_SIZE), MAX_POINT_PATH_LENGTH); };

	// result getters

	/**
	 * @brief Get the starting position of the path.
	 *
	 * @return The starting position as a Vector3.
	 */
	Vector3 getStartPosition()      const { return m_startPosition; }
	/**
	 * @brief Get the destination position of the path.
	 *
	 * @return The destination position as a Vector3.
	 */
	Vector3 getEndPosition()        const { return m_endPosition; }
	/**
	 * @brief Get the actual end position reached by the path.
	 *
	 * @return The actual end position as a Vector3.
	 */
	Vector3 getActualEndPosition()  const { return m_actualEndPosition; }

	/**
	 * @brief Get the calculated path.
	 *
	 * @return The calculated path as a PointsArray.
	 */
	PointsArray& getPath() { return m_pathPoints; }
	/**
	 * @brief Get the type of the calculated path.
	 *
	 * @return The path type as a PathType enumeration.
	 */
	PathType getPathType() const { return m_type; }

private:
	dtPolyRef m_pathPolyRefs[MAX_PATH_LENGTH];  /**< Array of detour polygon references. */
	unsigned int m_polyLength;                  /**< Number of polygons in the path. */

	PointsArray m_pathPoints;                   /**< Actual (x, y, z) path to the target. */
	PathType m_type;                            /**< Type of path (e.g., normal, shortcut). */

	bool m_useStraightPath;                     /**< Flag for the type of path to be generated. */
	bool m_forceDestination;                    /**< Flag to force arrival at the given point. */
	unsigned int m_pointPathLimit;              /**< Limit for point-based path size. */

	Vector3 m_startPosition;                    /**< {x, y, z} of the current location. */
	Vector3 m_endPosition;                      /**< {x, y, z} of the destination. */
	Vector3 m_actualEndPosition;                /**< {x, y, z} of the closest possible point to the given destination. */

	const unsigned int m_mapId;                 /**< Map ID. */
	const unsigned int m_instanceId;            /**< Instance ID. */
	const dtNavMesh* m_navMesh;                 /**< Pointer to the nav mesh. */
	const dtNavMeshQuery* m_navMeshQuery;       /**< Pointer to the nav mesh query used to find the path. */

	dtQueryFilter m_filter;                     /**< Filter used for pathfinding. */

	/**
	 * @brief Set the start position.
	 * @param point The new start position as a Vector3.
	 */
	void setStartPosition(const Vector3& point) { m_startPosition = point; }

	/**
	 * @brief Set the end position.
	 * @param point The new end position as a Vector3.
	 */
	void setEndPosition(const Vector3& point) { m_actualEndPosition = point; m_endPosition = point; }

	/**
	 * @brief Set the actual end position.
	 * @param point The new actual end position as a Vector3.
	 */
	void setActualEndPosition(const Vector3& point) { m_actualEndPosition = point; }

	/**
	 * @brief Clear the path data.
	 */
	void clear()
	{
		m_polyLength = 0;
		m_pathPoints.clear();
	}

	/**
	 * @brief Check if two points are within a specified range.
	 * @param p1 First point.
	 * @param p2 Second point.
	 * @param r Range.
	 * @param h Height.
	 * @return True if the points are within the range, false otherwise.
	 */
	bool inRange(const Vector3& p1, const Vector3& p2, float r, float h) const;

	/**
	 * @brief Calculate the square of the 3D distance between two points.
	 * @param p1 First point.
	 * @param p2 Second point.
	 * @return The square of the 3D distance between the points.
	 */
	float dist3DSqr(const Vector3& p1, const Vector3& p2) const;

	/**
	 * @brief Check if two points (YZX) are within a specified range.
	 * @param v1 First point (YZX).
	 * @param v2 Second point (YZX).
	 * @param r Range.
	 * @param h Height.
	 * @return True if the points are within the range, false otherwise.
	 */
	bool inRangeYZX(const float* v1, const float* v2, float r, float h) const;

	/**
	 * @brief Get the polygon reference of the path at a given position.
	 * @param polyPath Array of polygon references representing the path.
	 * @param polyPathSize Number of polygon references in the path.
	 * @param point The position to check.
	 * @param distance Pointer to store the distance.
	 * @return The polygon reference of the path at the given position.
	 */
	dtPolyRef getPathPolyByPosition(const dtPolyRef* polyPath, unsigned int polyPathSize, const float* point, float* distance = NULL) const;

	/**
	 * @brief Get the polygon reference at a given location.
	 * @param point The location to check.
	 * @param distance Pointer to store the distance.
	 * @return The polygon reference at the given location.
	 */
	dtPolyRef getPolyByLocation(const float* point, float* distance) const;

	/**
	 * @brief Check if a tile exists at a given position.
	 * @param p The position to check.
	 * @return True if a tile exists at the position, false otherwise.
	 */
	bool HaveTile(const Vector3& p) const;

	/**
	 * @brief Build the polygon path from the start to the end position.
	 * @param startPos The start position.
	 * @param endPos The end position.
	 */
	void BuildPolyPath(const Vector3& startPos, const Vector3& endPos);

	/**
	 * @brief Build the point-based path from the start to the end point.
	 * @param startPoint The start point.
	 * @param endPoint The end point.
	 */
	void BuildPointPath(const float* startPoint, const float* endPoint);

	/**
	 * @brief Build an error path.
	 */
	void BuildError();

	/**
	 * @brief Build a shortcut path.
	 */
	void BuildShortcut();

	/**
	 * @brief Get the navigation terrain type at a given position.
	 * @param x X-coordinate.
	 * @param y Y-coordinate.
	 * @param z Z-coordinate.
	 * @return The navigation terrain type.
	 */
	NavTerrain getNavTerrain(float x, float y, float z);

	/**
	 * @brief Create the query filter for pathfinding.
	 */
	void createFilter();

	/**
	 * @brief Update the query filter for pathfinding based on movement parameters.
	 * @param isSwimming Whether the entity is swimming.
	 * @param x X-coordinate.
	 * @param y Y-coordinate.
	 * @param z Z-coordinate.
	 */
	void updateFilter(bool isSwimming, float x, float y, float z);

	// Smooth path auxiliary functions

	/**
	 * @brief Fix up the corridor path to account for local geometric issues.
	 * @param path Array of polygon references representing the path.
	 * @param npath Number of polygon references in the path.
	 * @param maxPath Maximum number of polygon references for the path.
	 * @param visited Array of visited polygon references.
	 * @param nvisited Number of visited polygon references.
	 * @return The number of polygon references in the updated path.
	 */
	unsigned int fixupCorridor(dtPolyRef* path, unsigned int npath, unsigned int maxPath, const dtPolyRef* visited, unsigned int nvisited);
	/**
	 * @brief Get the steering target along the path.
	 * @param startPos The start position.
	 * @param endPos The end position.
	 * @param minTargetDist Minimum target distance.
	 * @param path Array of polygon references representing the path.
	 * @param pathSize Number of polygon references in the path.
	 * @param steerPos The resulting steering position.
	 * @param steerPosFlag Flag indicating the steering position type.
	 * @param steerPosRef Reference to the polygon reference of the steering position.
	 * @return True if a steering target is found, false otherwise.
	 */
	bool getSteerTarget(const float* startPos, const float* endPos, float minTargetDist, const dtPolyRef* path, unsigned int pathSize, float* steerPos, unsigned char& steerPosFlag, dtPolyRef& steerPosRef);
	/**
	 * @brief Find a smooth path between the start and end positions using the provided polygon path.
	 * @param startPos The start position.
	 * @param endPos The end position.
	 * @param polyPath Array of polygon references representing the path.
	 * @param polyPathSize Number of polygon references in the path.
	 * @param smoothPath Array to store the resulting smooth path.
	 * @param smoothPathSize Pointer to store the number of polygon references in the smooth path.
	 * @param smoothPathMaxSize Maximum number of polygon references for the smooth path.
	 * @return The status of the pathfinding operation.
	 */
	dtStatus findSmoothPath(const float* startPos, const float* endPos, const dtPolyRef* polyPath, unsigned int polyPathSize, float* smoothPath, int* smoothPathSize, unsigned int smoothPathMaxSize);
};

#endif
