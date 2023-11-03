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

#include <float.h>
#include <string.h>
#include "DetourNavMeshQuery.h"
#include "DetourNavMesh.h"
#include "DetourNode.h"
#include "DetourCommon.h"
#include "DetourMath.h"
#include "DetourAlloc.h"
#include "DetourAssert.h"
#include <new>

/// @class dtQueryFilter
///
/// <b>The Default Implementation</b>
/// 
/// At construction: All area costs default to 1.0.  All flags are included
/// and none are excluded.
/// 
/// If a polygon has both an include and an exclude flag, it will be excluded.
/// 
/// The way filtering works, a navigation mesh polygon must have at least one flag 
/// set to ever be considered by a query. So a polygon with no flags will never
/// be considered.
///
/// Setting the include flags to 0 will result in all polygons being excluded.
///
/// <b>Custom Implementations</b>
/// 
/// DT_VIRTUAL_QUERYFILTER must be defined in order to extend this class.
/// 
/// Implement a custom query filter by overriding the virtual passFilter() 
/// and getCost() functions. If this is done, both functions should be as 
/// fast as possible. Use cached local copies of data rather than accessing 
/// your own objects where possible.
/// 
/// Custom implementations do not need to adhere to the flags or cost logic 
/// used by the default implementation.  
/// 
/// In order for A* searches to work properly, the cost should be proportional to
/// the travel distance. Implementing a cost modifier less than 1.0 is likely 
/// to lead to problems during pathfinding.
///
/// @see dtNavMeshQuery

/**
 * @class dtQueryFilter
 *
 * @brief The default implementation of a query filter.
 *
 * This class represents a query filter used for pathfinding queries on a navigation mesh. It allows customization of area costs and flags to include or exclude certain polygons during pathfinding.
 *
 * @see dtNavMeshQuery
 */
dtQueryFilter::dtQueryFilter() :
	m_includeFlags(0xffff),
	m_excludeFlags(0)
{
	for (int i = 0; i < DT_MAX_AREAS; ++i)
		m_areaCost[i] = 1.0f;
}

/**
 * @brief Checks if a polygon passes the filter criteria.
 *
 * This function is used to determine if a polygon should be considered during a pathfinding query based on the filter's include and exclude flags.
 *
 * @param ref The polygon reference.
 * @param tile The tile containing the polygon.
 * @param poly The polygon to check.
 *
 * @return True if the polygon passes the filter, false otherwise.
 */
#ifdef DT_VIRTUAL_QUERYFILTER
bool dtQueryFilter::passFilter(const dtPolyRef /*ref*/,
	const dtMeshTile* /*tile*/,
	const dtPoly* poly) const
{
	return (poly->flags & m_includeFlags) != 0 && (poly->flags & m_excludeFlags) == 0;
}

/**
 * @brief Calculates the cost between two points on a polygon.
 *
 * This function calculates the cost of traveling between two points on a polygon based on the area cost and distance between the points.
 *
 * @param pa The start point.
 * @param pb The end point.
 * @param prevRef The reference to the previous polygon.
 * @param prevTile The tile containing the previous polygon.
 * @param prevPoly The previous polygon.
 * @param curRef The reference to the current polygon.
 * @param curTile The tile containing the current polygon.
 * @param curPoly The current polygon.
 * @param nextRef The reference to the next polygon.
 * @param nextTile The tile containing the next polygon.
 * @param nextPoly The next polygon.
 *
 * @return The cost of traveling between the points.
 */
float dtQueryFilter::getCost(const float* pa, const float* pb,
	const dtPolyRef /*prevRef*/, const dtMeshTile* /*prevTile*/, const dtPoly* /*prevPoly*/,
	const dtPolyRef /*curRef*/, const dtMeshTile* /*curTile*/, const dtPoly* curPoly,
	const dtPolyRef /*nextRef*/, const dtMeshTile* /*nextTile*/, const dtPoly* /*nextPoly*/) const
{
	return dtVdist(pa, pb) * m_areaCost[curPoly->getArea()];
}
#else
 /**
  * @brief Checks if a polygon passes the filter criteria.
  *
  * This function is used to determine if a polygon should be considered during a pathfinding query based on the filter's include and exclude flags.
  *
  * @param ref The polygon reference.
  * @param tile The tile containing the polygon.
  * @param poly The polygon to check.
  *
  * @return True if the polygon passes the filter, false otherwise.
  */
inline bool dtQueryFilter::passFilter(const dtPolyRef /*ref*/,
	const dtMeshTile* /*tile*/,
	const dtPoly* poly) const
{
	return (poly->flags & m_includeFlags) != 0 && (poly->flags & m_excludeFlags) == 0;
}

/**
 * @brief Calculates the cost between two points on a polygon.
 *
 * This function calculates the cost of traveling between two points on a polygon based on the area cost and distance between the points.
 *
 * @param pa The start point.
 * @param pb The end point.
 * @param prevRef The reference to the previous polygon.
 * @param prevTile The tile containing the previous polygon.
 * @param prevPoly The previous polygon.
 * @param curRef The reference to the current polygon.
 * @param curTile The tile containing the current polygon.
 * @param curPoly The current polygon.
 * @param nextRef The reference to the next polygon.
 * @param nextTile The tile containing the next polygon.
 * @param nextPoly The next polygon.
 *
 * @return The cost of traveling between the points.
 */
inline float dtQueryFilter::getCost(const float* pa, const float* pb,
	const dtPolyRef /*prevRef*/, const dtMeshTile* /*prevTile*/, const dtPoly* /*prevPoly*/,
	const dtPolyRef /*curRef*/, const dtMeshTile* /*curTile*/, const dtPoly* curPoly,
	const dtPolyRef /*nextRef*/, const dtMeshTile* /*nextTile*/, const dtPoly* /*nextPoly*/) const
{
	return dtVdist(pa, pb) * m_areaCost[curPoly->getArea()];
}
#endif	

/**
 * @brief A constant representing the scale of the search heuristic.
 *
 * The search heuristic scale is used in pathfinding calculations.
 */
static const float H_SCALE = 0.999f; // Search heuristic scale.

/**
 * @brief Allocates memory for a new navigation mesh query object.
 *
 * This function allocates memory for a new navigation mesh query object and initializes it.
 *
 * @return A pointer to the newly allocated navigation mesh query object.
 */
dtNavMeshQuery* dtAllocNavMeshQuery()
{
	void* mem = dtAlloc(sizeof(dtNavMeshQuery), DT_ALLOC_PERM);
	if (!mem) return 0;
	return new(mem) dtNavMeshQuery;
}

/**
 * @brief Frees memory allocated for a navigation mesh query object.
 *
 * This function releases the memory allocated for a navigation mesh query object.
 *
 * @param navmesh A pointer to the navigation mesh query object to be freed.
 */
void dtFreeNavMeshQuery(dtNavMeshQuery* navmesh)
{
	if (!navmesh) return;
	navmesh->~dtNavMeshQuery();
	dtFree(navmesh);
}

//////////////////////////////////////////////////////////////////////////////////////////

/// @class dtNavMeshQuery
///
/// For methods that support undersized buffers, if the buffer is too small 
/// to hold the entire result set the return status of the method will include 
/// the #DT_BUFFER_TOO_SMALL flag.
///
/// Constant member functions can be used by multiple clients without side
/// effects. (E.g. No change to the closed list. No impact on an in-progress
/// sliced path query. Etc.)
/// 
/// Walls and portals: A @e wall is a polygon segment that is 
/// considered impassable. A @e portal is a passable segment between polygons.
/// A portal may be treated as a wall based on the dtQueryFilter used for a query.
///
/// @see dtNavMesh, dtQueryFilter, #dtAllocNavMeshQuery(), #dtAllocNavMeshQuery()

/**
 * @class dtNavMeshQuery
 *
 * @brief Represents a query object for navigation mesh queries.
 *
 * This class provides functionality for performing various types of pathfinding queries on a navigation mesh.
 *
 * @see dtNavMesh, dtQueryFilter, #dtAllocNavMeshQuery(), #dtAllocNavMeshQuery()
 */
dtNavMeshQuery::dtNavMeshQuery() :
	m_nav(0),
	m_tinyNodePool(0),
	m_nodePool(0),
	m_openList(0)
{
	memset(&m_query, 0, sizeof(dtQueryData));
}

/**
 * @brief Destroys the navigation mesh query object and frees allocated memory.
 */
dtNavMeshQuery::~dtNavMeshQuery()
{
	if (m_tinyNodePool)
		m_tinyNodePool->~dtNodePool();
	if (m_nodePool)
		m_nodePool->~dtNodePool();
	if (m_openList)
		m_openList->~dtNodeQueue();
	dtFree(m_tinyNodePool);
	dtFree(m_nodePool);
	dtFree(m_openList);
}

/**
 * @brief Initializes the navigation mesh query object.
 *
 * This function initializes the navigation mesh query object with the specified navigation mesh and maximum number of nodes.
 * Must be the first function called after construction, before other functions are used.  Can be called multiple times.
 *
 * @param nav The navigation mesh to use for queries.
 * @param maxNodes The maximum number of nodes to allocate for queries.
 *
 * @return The status of the initialization operation.
 */
dtStatus dtNavMeshQuery::init(const dtNavMesh* nav, const int maxNodes)
{
	m_nav = nav;

	if (!m_nodePool || m_nodePool->getMaxNodes() < maxNodes)
	{
		if (m_nodePool)
		{
			m_nodePool->~dtNodePool();
			dtFree(m_nodePool);
			m_nodePool = 0;
		}
		m_nodePool = new (dtAlloc(sizeof(dtNodePool), DT_ALLOC_PERM)) dtNodePool(maxNodes, dtNextPow2(maxNodes / 4));
		if (!m_nodePool)
			return DT_FAILURE | DT_OUT_OF_MEMORY;
	}
	else
	{
		m_nodePool->clear();
	}

	if (!m_tinyNodePool)
	{
		m_tinyNodePool = new (dtAlloc(sizeof(dtNodePool), DT_ALLOC_PERM)) dtNodePool(64, 32);
		if (!m_tinyNodePool)
			return DT_FAILURE | DT_OUT_OF_MEMORY;
	}
	else
	{
		m_tinyNodePool->clear();
	}

	// TODO: check the open list size too.
	if (!m_openList || m_openList->getCapacity() < maxNodes)
	{
		if (m_openList)
		{
			m_openList->~dtNodeQueue();
			dtFree(m_openList);
			m_openList = 0;
		}
		m_openList = new (dtAlloc(sizeof(dtNodeQueue), DT_ALLOC_PERM)) dtNodeQueue(maxNodes);
		if (!m_openList)
			return DT_FAILURE | DT_OUT_OF_MEMORY;
	}
	else
	{
		m_openList->clear();
	}

	return DT_SUCCESS;
}

/**
 * @brief Finds a random point on the navigation mesh.
 *
 * This function randomly selects a point on the navigation mesh using a reservoir sampling method. It chooses a random tile first and then a random polygon within that tile, weighted by their areas.
 *
 * @param filter The query filter to apply to the random point selection.
 * @param frand A function that generates random values between 0 and 1.
 * @param randomRef The resulting random polygon reference.
 * @param randomPt The resulting random point on the navigation mesh.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::findRandomPoint(const dtQueryFilter* filter, float (*frand)(),
	dtPolyRef* randomRef, float* randomPt) const
{
	dtAssert(m_nav);

	// Randomly pick one tile. Assume that all tiles cover roughly the same area.
	const dtMeshTile* tile = 0;
	float tsum = 0.0f;
	for (int i = 0; i < m_nav->getMaxTiles(); i++)
	{
		const dtMeshTile* t = m_nav->getTile(i);
		if (!t || !t->header) continue;

		// Choose random tile using reservoi sampling.
		const float area = 1.0f; // Could be tile area too.
		tsum += area;
		const float u = frand();
		if (u * tsum <= area)
			tile = t;
	}
	if (!tile)
		return DT_FAILURE;

	// Randomly pick one polygon weighted by polygon area.
	const dtPoly* poly = 0;
	dtPolyRef polyRef = 0;
	const dtPolyRef base = m_nav->getPolyRefBase(tile);

	float areaSum = 0.0f;
	for (int i = 0; i < tile->header->polyCount; ++i)
	{
		const dtPoly* p = &tile->polys[i];
		// Do not return off-mesh connection polygons.
		if (p->getType() != DT_POLYTYPE_GROUND)
			continue;
		// Must pass filter
		const dtPolyRef ref = base | (dtPolyRef)i;
		if (!filter->passFilter(ref, tile, p))
			continue;

		// Calc area of the polygon.
		float polyArea = 0.0f;
		for (int j = 2; j < p->vertCount; ++j)
		{
			const float* va = &tile->verts[p->verts[0] * 3];
			const float* vb = &tile->verts[p->verts[j - 1] * 3];
			const float* vc = &tile->verts[p->verts[j] * 3];
			polyArea += dtTriArea2D(va, vb, vc);
		}

		// Choose random polygon weighted by area, using reservoi sampling.
		areaSum += polyArea;
		const float u = frand();
		if (u * areaSum <= polyArea)
		{
			poly = p;
			polyRef = ref;
		}
	}

	if (!poly)
		return DT_FAILURE;

	// Randomly pick point on polygon.
	const float* v = &tile->verts[poly->verts[0] * 3];
	float verts[3 * DT_VERTS_PER_POLYGON];
	float areas[DT_VERTS_PER_POLYGON];
	dtVcopy(&verts[0 * 3], v);
	for (int j = 1; j < poly->vertCount; ++j)
	{
		v = &tile->verts[poly->verts[j] * 3];
		dtVcopy(&verts[j * 3], v);
	}

	const float s = frand();
	const float t = frand();

	float pt[3];
	dtRandomPointInConvexPoly(verts, poly->vertCount, areas, s, t, pt);

	float h = 0.0f;
	dtStatus status = getPolyHeight(polyRef, pt, &h);
	if (dtStatusFailed(status))
		return status;
	pt[1] = h;

	dtVcopy(randomPt, pt);
	*randomRef = polyRef;

	return DT_SUCCESS;
}

/**
 * @brief Finds a random point around a circle on the navigation mesh.
 *
 * This function randomly selects a point on the navigation mesh around a specified circle centered at `centerPos`. It uses a reservoir sampling method to choose a random polygon within the circle, weighted by their areas.
 *
 * @param startRef The reference of the starting polygon within the circle.
 * @param centerPos The center position of the circle.
 * @param radius The radius of the circle.
 * @param filter The query filter to apply to the random point selection.
 * @param frand A function that generates random values between 0 and 1.
 * @param randomRef The resulting random polygon reference.
 * @param randomPt The resulting random point on the navigation mesh.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::findRandomPointAroundCircle(dtPolyRef startRef, const float* centerPos, const float radius,
	const dtQueryFilter* filter, float (*frand)(),
	dtPolyRef* randomRef, float* randomPt) const
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	const dtMeshTile* startTile = 0;
	const dtPoly* startPoly = 0;
	m_nav->getTileAndPolyByRefUnsafe(startRef, &startTile, &startPoly);
	if (!filter->passFilter(startRef, startTile, startPoly))
		return DT_FAILURE | DT_INVALID_PARAM;

	m_nodePool->clear();
	m_openList->clear();

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, centerPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	dtStatus status = DT_SUCCESS;

	const float radiusSqr = dtSqr(radius);
	float areaSum = 0.0f;

	const dtMeshTile* randomTile = 0;
	const dtPoly* randomPoly = 0;
	dtPolyRef randomPolyRef = 0;

	while (!m_openList->empty())
	{
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(bestRef, &bestTile, &bestPoly);

		// Place random locations on on ground.
		if (bestPoly->getType() == DT_POLYTYPE_GROUND)
		{
			// Calc area of the polygon.
			float polyArea = 0.0f;
			for (int j = 2; j < bestPoly->vertCount; ++j)
			{
				const float* va = &bestTile->verts[bestPoly->verts[0] * 3];
				const float* vb = &bestTile->verts[bestPoly->verts[j - 1] * 3];
				const float* vc = &bestTile->verts[bestPoly->verts[j] * 3];
				polyArea += dtTriArea2D(va, vb, vc);
			}
			// Choose random polygon weighted by area, using reservoi sampling.
			areaSum += polyArea;
			const float u = frand();
			if (u * areaSum <= polyArea)
			{
				randomTile = bestTile;
				randomPoly = bestPoly;
				randomPolyRef = bestRef;
			}
		}


		// Get parent poly and tile.
		dtPolyRef parentRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		if (bestNode->pidx)
			parentRef = m_nodePool->getNodeAtIdx(bestNode->pidx)->id;
		if (parentRef)
			m_nav->getTileAndPolyByRefUnsafe(parentRef, &parentTile, &parentPoly);

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			const dtLink* link = &bestTile->links[i];
			dtPolyRef neighbourRef = link->ref;
			// Skip invalid neighbours and do not follow back to parent.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Expand to neighbour
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			// Do not advance if the polygon is excluded by the filter.
			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// Find edge and calc distance to the edge.
			float va[3], vb[3];
			if (!getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb))
				continue;

			// If the circle is not touching the next polygon, skip it.
			float tseg;
			float distSqr = dtDistancePtSegSqr2D(centerPos, va, vb, tseg);
			if (distSqr > radiusSqr)
				continue;

			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef);
			if (!neighbourNode)
			{
				status |= DT_OUT_OF_NODES;
				continue;
			}

			if (neighbourNode->flags & DT_NODE_CLOSED)
				continue;

			// Cost
			if (neighbourNode->flags == 0)
				dtVlerp(neighbourNode->pos, va, vb, 0.5f);

			const float total = bestNode->total + dtVdist(bestNode->pos, neighbourNode->pos);

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;

			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~DT_NODE_CLOSED);
			neighbourNode->pidx = m_nodePool->getNodeIdx(bestNode);
			neighbourNode->total = total;

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				m_openList->modify(neighbourNode);
			}
			else
			{
				neighbourNode->flags = DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}
		}
	}

	if (!randomPoly)
		return DT_FAILURE;

	// Randomly pick point on polygon.
	const float* v = &randomTile->verts[randomPoly->verts[0] * 3];
	float verts[3 * DT_VERTS_PER_POLYGON];
	float areas[DT_VERTS_PER_POLYGON];
	dtVcopy(&verts[0 * 3], v);
	for (int j = 1; j < randomPoly->vertCount; ++j)
	{
		v = &randomTile->verts[randomPoly->verts[j] * 3];
		dtVcopy(&verts[j * 3], v);
	}

	const float s = frand();
	const float t = frand();

	float pt[3];
	dtRandomPointInConvexPoly(verts, randomPoly->vertCount, areas, s, t, pt);

	float h = 0.0f;
	dtStatus stat = getPolyHeight(randomPolyRef, pt, &h);
	if (dtStatusFailed(status))
		return stat;
	pt[1] = h;

	dtVcopy(randomPt, pt);
	*randomRef = randomPolyRef;

	return DT_SUCCESS;
}

//////////////////////////////////////////////////////////////////////////////////////////

/**
 * @brief Finds the closest point on a polygon to a given position.
 *
 * This function calculates the closest point on a specified polygon to a given position. It handles both ground polygons and off-mesh connections.
 *
 * @param ref The reference of the polygon.
 * @param pos The position to find the closest point to.
 * @param closest The resulting closest point on the polygon.
 * @param posOverPoly A pointer to a boolean flag indicating if the provided position is over the polygon (optional).
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::closestPointOnPoly(dtPolyRef ref, const float* pos, float* closest, bool* posOverPoly) const
{
	dtAssert(m_nav);
	const dtMeshTile* tile = 0;
	const dtPoly* poly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(ref, &tile, &poly)))
		return DT_FAILURE | DT_INVALID_PARAM;
	if (!tile)
		return DT_FAILURE | DT_INVALID_PARAM;

	// Off-mesh connections don't have detail polygons.
	if (poly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
	{
		const float* v0 = &tile->verts[poly->verts[0] * 3];
		const float* v1 = &tile->verts[poly->verts[1] * 3];
		const float d0 = dtVdist(pos, v0);
		const float d1 = dtVdist(pos, v1);
		const float u = d0 / (d0 + d1);
		dtVlerp(closest, v0, v1, u);
		if (posOverPoly)
			*posOverPoly = false;
		return DT_SUCCESS;
	}

	const unsigned int ip = (unsigned int)(poly - tile->polys);
	const dtPolyDetail* pd = &tile->detailMeshes[ip];

	// Clamp point to be inside the polygon.
	float verts[DT_VERTS_PER_POLYGON * 3];
	float edged[DT_VERTS_PER_POLYGON];
	float edget[DT_VERTS_PER_POLYGON];
	const int nv = poly->vertCount;
	for (int i = 0; i < nv; ++i)
		dtVcopy(&verts[i * 3], &tile->verts[poly->verts[i] * 3]);

	dtVcopy(closest, pos);
	if (!dtDistancePtPolyEdgesSqr(pos, verts, nv, edged, edget))
	{
		// Point is outside the polygon, dtClamp to nearest edge.
		float dmin = FLT_MAX;
		int imin = -1;
		for (int i = 0; i < nv; ++i)
		{
			if (edged[i] < dmin)
			{
				dmin = edged[i];
				imin = i;
			}
		}
		const float* va = &verts[imin * 3];
		const float* vb = &verts[((imin + 1) % nv) * 3];
		dtVlerp(closest, va, vb, edget[imin]);

		if (posOverPoly)
			*posOverPoly = false;
	}
	else
	{
		if (posOverPoly)
			*posOverPoly = true;
	}

	// Find height at the location.
	for (int j = 0; j < pd->triCount; ++j)
	{
		const unsigned char* t = &tile->detailTris[(pd->triBase + j) * 4];
		const float* v[3];
		for (int k = 0; k < 3; ++k)
		{
			if (t[k] < poly->vertCount)
				v[k] = &tile->verts[poly->verts[t[k]] * 3];
			else
				v[k] = &tile->detailVerts[(pd->vertBase + (t[k] - poly->vertCount)) * 3];
		}
		float h;
		if (dtClosestHeightPointTriangle(pos, v[0], v[1], v[2], h))
		{
			closest[1] = h;
			break;
		}
	}

	return DT_SUCCESS;
}

/**
 * @brief Finds the closest point on the boundary of a polygon to a given position.
 *
 * This function calculates the closest point on the boundary of a specified polygon to a given position. It is much faster than `closestPointOnPoly()` but does not account for height detail.
 *
 * If the provided position lies within the polygon's xz-bounds (above or below), then `pos` and `closest` will be equal.
 *
 * The height of `closest` will be the polygon boundary. The height detail is not used.
 *
 * `pos` does not have to be within the bounds of the polygon or the navigation mesh.
 *
 * @param ref The reference of the polygon.
 * @param pos The position to find the closest point to.
 * @param closest The resulting closest point on the polygon's boundary.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::closestPointOnPolyBoundary(dtPolyRef ref, const float* pos, float* closest) const
{
	dtAssert(m_nav);

	const dtMeshTile* tile = 0;
	const dtPoly* poly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(ref, &tile, &poly)))
		return DT_FAILURE | DT_INVALID_PARAM;

	// Collect vertices.
	float verts[DT_VERTS_PER_POLYGON * 3];
	float edged[DT_VERTS_PER_POLYGON];
	float edget[DT_VERTS_PER_POLYGON];
	int nv = 0;
	for (int i = 0; i < (int)poly->vertCount; ++i)
	{
		dtVcopy(&verts[nv * 3], &tile->verts[poly->verts[i] * 3]);
		nv++;
	}

	bool inside = dtDistancePtPolyEdgesSqr(pos, verts, nv, edged, edget);
	if (inside)
	{
		// Point is inside the polygon, return the point.
		dtVcopy(closest, pos);
	}
	else
	{
		// Point is outside the polygon, dtClamp to nearest edge.
		float dmin = FLT_MAX;
		int imin = -1;
		for (int i = 0; i < nv; ++i)
		{
			if (edged[i] < dmin)
			{
				dmin = edged[i];
				imin = i;
			}
		}
		const float* va = &verts[imin * 3];
		const float* vb = &verts[((imin + 1) % nv) * 3];
		dtVlerp(closest, va, vb, edget[imin]);
	}

	return DT_SUCCESS;
}

/**
 * @brief Gets the height of a position within a polygon.
 *
 * This function calculates the height of a position within a specified polygon. It considers both ground polygons and off-mesh connections.
 *
 * @param ref The reference of the polygon.
 * @param pos The position to find the height at.
 * @param height The resulting height at the position.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::getPolyHeight(dtPolyRef ref, const float* pos, float* height) const
{
	dtAssert(m_nav);

	const dtMeshTile* tile = 0;
	const dtPoly* poly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(ref, &tile, &poly)))
		return DT_FAILURE | DT_INVALID_PARAM;

	if (poly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
	{
		const float* v0 = &tile->verts[poly->verts[0] * 3];
		const float* v1 = &tile->verts[poly->verts[1] * 3];
		const float d0 = dtVdist2D(pos, v0);
		const float d1 = dtVdist2D(pos, v1);
		const float u = d0 / (d0 + d1);
		if (height)
			*height = v0[1] + (v1[1] - v0[1]) * u;
		return DT_SUCCESS;
	}
	else
	{
		const unsigned int ip = (unsigned int)(poly - tile->polys);
		const dtPolyDetail* pd = &tile->detailMeshes[ip];
		for (int j = 0; j < pd->triCount; ++j)
		{
			const unsigned char* t = &tile->detailTris[(pd->triBase + j) * 4];
			const float* v[3];
			for (int k = 0; k < 3; ++k)
			{
				if (t[k] < poly->vertCount)
					v[k] = &tile->verts[poly->verts[t[k]] * 3];
				else
					v[k] = &tile->detailVerts[(pd->vertBase + (t[k] - poly->vertCount)) * 3];
			}
			float h;
			if (dtClosestHeightPointTriangle(pos, v[0], v[1], v[2], h))
			{
				if (height)
					*height = h;
				return DT_SUCCESS;
			}
		}
	}

	return DT_FAILURE | DT_INVALID_PARAM;
}

/**
 * @brief Finds the nearest polygon to a given center point within specified extents.
 *
 * This function finds the nearest polygon to a specified center point within the specified extents. It uses a query filter to determine if a polygon is valid. The nearest point on the polygon's boundary is also provided.
 *
 * If the search box does not intersect any polygons, the search will return DT_SUCCESS, but `nearestRef` will be zero. So if in doubt, check `nearestRef` before using `nearestPt`.
 *
 * @warning This function is not suitable for large area searches. If the search extents overlap more than 128 polygons, it may return an invalid result.
 *
 * @param center The center point for the search.
 * @param extents The extents of the search box.
 * @param filter The query filter to apply to the search.
 * @param nearestRef The resulting reference of the nearest polygon.
 * @param nearestPt The resulting nearest point on the polygon's boundary.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::findNearestPoly(const float* center, const float* extents,
	const dtQueryFilter* filter,
	dtPolyRef* nearestRef, float* nearestPt) const
{
	dtAssert(m_nav);

	*nearestRef = 0;

	// Get nearby polygons from proximity grid.
	dtPolyRef polys[128];
	int polyCount = 0;
	if (dtStatusFailed(queryPolygons(center, extents, filter, polys, &polyCount, 128)))
		return DT_FAILURE | DT_INVALID_PARAM;

	// Find nearest polygon amongst the nearby polygons.
	dtPolyRef nearest = 0;
	float nearestDistanceSqr = FLT_MAX;
	for (int i = 0; i < polyCount; ++i)
	{
		dtPolyRef ref = polys[i];
		float closestPtPoly[3];
		float diff[3];
		bool posOverPoly = false;
		float d = 0;
		closestPointOnPoly(ref, center, closestPtPoly, &posOverPoly);

		// If a point is directly over a polygon and closer than
		// climb height, favor that instead of straight line nearest point.
		dtVsub(diff, center, closestPtPoly);
		if (posOverPoly)
		{
			const dtMeshTile* tile = 0;
			const dtPoly* poly = 0;
			m_nav->getTileAndPolyByRefUnsafe(polys[i], &tile, &poly);
			d = dtAbs(diff[1]) - tile->header->walkableClimb;
			d = d > 0 ? d * d : 0;
		}
		else
		{
			d = dtVlenSqr(diff);
		}

		if (d < nearestDistanceSqr)
		{
			if (nearestPt)
				dtVcopy(nearestPt, closestPtPoly);
			nearestDistanceSqr = d;
			nearest = ref;
		}
	}

	if (nearestRef)
		*nearestRef = nearest;

	return DT_SUCCESS;
}

/**
 * @brief Queries polygons within a tile that intersect with a specified bounding box.
 *
 * This function queries polygons within a specified tile that intersect with a bounding box defined by `qmin` and `qmax`. It uses a query filter to determine if a polygon is valid. The matching polygons' references are stored in the `polys` array, up to a maximum of `maxPolys`.
 *
 * @param tile The tile to query polygons from.
 * @param qmin The minimum bounds of the query box.
 * @param qmax The maximum bounds of the query box.
 * @param filter The query filter to apply to the polygons.
 * @param polys An array to store the references of matching polygons.
 * @param maxPolys The maximum number of polygon references to store.
 *
 * @return The number of matching polygons found.
 */
int dtNavMeshQuery::queryPolygonsInTile(const dtMeshTile* tile, const float* qmin, const float* qmax,
	const dtQueryFilter* filter,
	dtPolyRef* polys, const int maxPolys) const
{
	dtAssert(m_nav);

	if (tile->bvTree)
	{
		const dtBVNode* node = &tile->bvTree[0];
		const dtBVNode* end = &tile->bvTree[tile->header->bvNodeCount];
		const float* tbmin = tile->header->bmin;
		const float* tbmax = tile->header->bmax;
		const float qfac = tile->header->bvQuantFactor;

		// Calculate quantized box
		unsigned short bmin[3], bmax[3];
		// dtClamp query box to world box.
		float minx = dtClamp(qmin[0], tbmin[0], tbmax[0]) - tbmin[0];
		float miny = dtClamp(qmin[1], tbmin[1], tbmax[1]) - tbmin[1];
		float minz = dtClamp(qmin[2], tbmin[2], tbmax[2]) - tbmin[2];
		float maxx = dtClamp(qmax[0], tbmin[0], tbmax[0]) - tbmin[0];
		float maxy = dtClamp(qmax[1], tbmin[1], tbmax[1]) - tbmin[1];
		float maxz = dtClamp(qmax[2], tbmin[2], tbmax[2]) - tbmin[2];
		// Quantize
		bmin[0] = (unsigned short)(qfac * minx) & 0xfffe;
		bmin[1] = (unsigned short)(qfac * miny) & 0xfffe;
		bmin[2] = (unsigned short)(qfac * minz) & 0xfffe;
		bmax[0] = (unsigned short)(qfac * maxx + 1) | 1;
		bmax[1] = (unsigned short)(qfac * maxy + 1) | 1;
		bmax[2] = (unsigned short)(qfac * maxz + 1) | 1;

		// Traverse tree
		const dtPolyRef base = m_nav->getPolyRefBase(tile);
		int n = 0;
		while (node < end)
		{
			const bool overlap = dtOverlapQuantBounds(bmin, bmax, node->bmin, node->bmax);
			const bool isLeafNode = node->i >= 0;

			if (isLeafNode && overlap)
			{
				dtPolyRef ref = base | (dtPolyRef)node->i;
				if (filter->passFilter(ref, tile, &tile->polys[node->i]))
				{
					if (n < maxPolys)
						polys[n++] = ref;
				}
			}

			if (overlap || isLeafNode)
				node++;
			else
			{
				const int escapeIndex = -node->i;
				node += escapeIndex;
			}
		}

		return n;
	}
	else
	{
		float bmin[3], bmax[3];
		int n = 0;
		const dtPolyRef base = m_nav->getPolyRefBase(tile);
		for (int i = 0; i < tile->header->polyCount; ++i)
		{
			const dtPoly* p = &tile->polys[i];
			// Do not return off-mesh connection polygons.
			if (p->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
				continue;
			// Must pass filter
			const dtPolyRef ref = base | (dtPolyRef)i;
			if (!filter->passFilter(ref, tile, p))
				continue;
			// Calc polygon bounds.
			const float* v = &tile->verts[p->verts[0] * 3];
			dtVcopy(bmin, v);
			dtVcopy(bmax, v);
			for (int j = 1; j < p->vertCount; ++j)
			{
				v = &tile->verts[p->verts[j] * 3];
				dtVmin(bmin, v);
				dtVmax(bmax, v);
			}
			if (dtOverlapBounds(qmin, qmax, bmin, bmax))
			{
				if (n < maxPolys)
					polys[n++] = ref;
			}
		}
		return n;
	}
}

/**
 * @brief Queries polygons within a specified bounding box.
 *
 * This function queries polygons within a specified bounding box defined by `center` and `extents`. It uses a query filter to determine if a polygon is valid. The matching polygons' references are stored in the `polys` array, up to a maximum of `maxPolys`.
 *
 * If no polygons are found, the function will return #DT_SUCCESS with a `polyCount` of zero.
 *
 * If `polys` is too small to hold the entire result set, then the array will be filled to capacity. The method of choosing which polygons from the full set are included in the partial result set is undefined.
 *
 * @param center The center point of the query bounding box.
 * @param extents The half extents of the query bounding box.
 * @param filter The query filter to apply to the polygons.
 * @param polys An array to store the references of matching polygons.
 * @param polyCount A pointer to store the number of matching polygons found.
 * @param maxPolys The maximum number of polygon references to store.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::queryPolygons(const float* center, const float* extents,
	const dtQueryFilter* filter,
	dtPolyRef* polys, int* polyCount, const int maxPolys) const
{
	dtAssert(m_nav);

	float bmin[3], bmax[3];
	dtVsub(bmin, center, extents);
	dtVadd(bmax, center, extents);

	// Find tiles the query touches.
	int minx, miny, maxx, maxy;
	m_nav->calcTileLoc(bmin, &minx, &miny);
	m_nav->calcTileLoc(bmax, &maxx, &maxy);

	static const int MAX_NEIS = 32;
	const dtMeshTile* neis[MAX_NEIS];

	int n = 0;
	for (int y = miny; y <= maxy; ++y)
	{
		for (int x = minx; x <= maxx; ++x)
		{
			const int nneis = m_nav->getTilesAt(x, y, neis, MAX_NEIS);
			for (int j = 0; j < nneis; ++j)
			{
				n += queryPolygonsInTile(neis[j], bmin, bmax, filter, polys + n, maxPolys - n);
				if (n >= maxPolys)
				{
					*polyCount = n;
					return DT_SUCCESS | DT_BUFFER_TOO_SMALL;
				}
			}
		}
	}
	*polyCount = n;

	return DT_SUCCESS;
}

/**
 * @brief Finds a path from a start polygon to an end polygon.
 *
 * This function finds a path from a specified start polygon to a specified end polygon. It uses A* search to calculate the path, considering traversal costs and a query filter to determine valid paths.
 *
 * If the end polygon cannot be reached through the navigation graph, the last polygon in the path will be the nearest to the end polygon.
 *
 * If the path array is too small to hold the full result, it will be filled as far as possible from the start polygon toward the end polygon.
 *
 * The start and end positions are used to calculate traversal costs. The y-values impact the result.
 *
 * @param startRef The reference of the start polygon.
 * @param endRef The reference of the end polygon.
 * @param startPos The position of the start point.
 * @param endPos The position of the end point.
 * @param filter The query filter to apply to the pathfinding.
 * @param path An array to store the references of the path polygons.
 * @param pathCount A pointer to store the number of path polygons found.
 * @param maxPath The maximum number of polygon references to store in the path array.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::findPath(dtPolyRef startRef, dtPolyRef endRef,
	const float* startPos, const float* endPos,
	const dtQueryFilter* filter,
	dtPolyRef* path, int* pathCount, const int maxPath) const
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	*pathCount = 0;

	if (!startRef || !endRef)
		return DT_FAILURE | DT_INVALID_PARAM;

	if (!maxPath)
		return DT_FAILURE | DT_INVALID_PARAM;

	// Validate input
	if (!m_nav->isValidPolyRef(startRef) || !m_nav->isValidPolyRef(endRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	if (startRef == endRef)
	{
		path[0] = startRef;
		*pathCount = 1;
		return DT_SUCCESS;
	}

	m_nodePool->clear();
	m_openList->clear();

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, startPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = dtVdist(startPos, endPos) * H_SCALE;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	dtNode* lastBestNode = startNode;
	float lastBestNodeCost = startNode->total;

	dtStatus status = DT_SUCCESS;

	while (!m_openList->empty())
	{
		// Remove node from open list and put it in closed list.
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Reached the goal, stop searching.
		if (bestNode->id == endRef)
		{
			lastBestNode = bestNode;
			break;
		}

		// Get current poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(bestRef, &bestTile, &bestPoly);

		// Get parent poly and tile.
		dtPolyRef parentRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		if (bestNode->pidx)
			parentRef = m_nodePool->getNodeAtIdx(bestNode->pidx)->id;
		if (parentRef)
			m_nav->getTileAndPolyByRefUnsafe(parentRef, &parentTile, &parentPoly);

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			dtPolyRef neighbourRef = bestTile->links[i].ref;

			// Skip invalid ids and do not expand back to where we came from.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Get neighbour poly and tile.
			// The API input has been cheked already, skip checking internal data.
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// deal explicitly with crossing tile boundaries
			unsigned char crossSide = 0;
			if (bestTile->links[i].side != 0xff)
				crossSide = bestTile->links[i].side >> 1;

			// get the node
			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef, crossSide);
			if (!neighbourNode)
			{
				status |= DT_OUT_OF_NODES;
				continue;
			}

			// If the node is visited the first time, calculate node position.
			if (neighbourNode->flags == 0)
			{
				getEdgeMidPoint(bestRef, bestPoly, bestTile,
					neighbourRef, neighbourPoly, neighbourTile,
					neighbourNode->pos);
			}

			// Calculate cost and heuristic.
			float cost = 0;
			float heuristic = 0;

			// Special case for last node.
			if (neighbourRef == endRef)
			{
				// Cost
				const float curCost = filter->getCost(bestNode->pos, neighbourNode->pos,
					parentRef, parentTile, parentPoly,
					bestRef, bestTile, bestPoly,
					neighbourRef, neighbourTile, neighbourPoly);
				const float endCost = filter->getCost(neighbourNode->pos, endPos,
					bestRef, bestTile, bestPoly,
					neighbourRef, neighbourTile, neighbourPoly,
					0, 0, 0);

				cost = bestNode->cost + curCost + endCost;
				heuristic = 0;
			}
			else
			{
				// Cost
				const float curCost = filter->getCost(bestNode->pos, neighbourNode->pos,
					parentRef, parentTile, parentPoly,
					bestRef, bestTile, bestPoly,
					neighbourRef, neighbourTile, neighbourPoly);
				cost = bestNode->cost + curCost;
				heuristic = dtVdist(neighbourNode->pos, endPos) * H_SCALE;
			}

			const float total = cost + heuristic;

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;
			// The node is already visited and process, and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_CLOSED) && total >= neighbourNode->total)
				continue;

			// Add or update the node.
			neighbourNode->pidx = m_nodePool->getNodeIdx(bestNode);
			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~DT_NODE_CLOSED);
			neighbourNode->cost = cost;
			neighbourNode->total = total;

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				// Already in open, update node location.
				m_openList->modify(neighbourNode);
			}
			else
			{
				// Put the node in open list.
				neighbourNode->flags |= DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}

			// Update nearest node to target so far.
			if (heuristic < lastBestNodeCost)
			{
				lastBestNodeCost = heuristic;
				lastBestNode = neighbourNode;
			}
		}
	}

	if (lastBestNode->id != endRef)
		status |= DT_PARTIAL_RESULT;

	// Reverse the path.
	dtNode* prev = 0;
	dtNode* node = lastBestNode;
	do
	{
		dtNode* next = m_nodePool->getNodeAtIdx(node->pidx);
		node->pidx = m_nodePool->getNodeIdx(prev);
		prev = node;
		node = next;
	} while (node);

	// Store path
	node = prev;
	int n = 0;
	do
	{
		path[n++] = node->id;
		if (n >= maxPath)
		{
			status |= DT_BUFFER_TOO_SMALL;
			break;
		}
		node = m_nodePool->getNodeAtIdx(node->pidx);
	} while (node);

	*pathCount = n;

	return status;
}

/**
 * @brief Initializes a sliced pathfinding query.
 *
 * This function initializes a sliced pathfinding query with the specified parameters. It is part of a multi-step process to find a path from a start polygon to an end polygon in a navigation mesh. This function sets up the query and prepares it for execution.
 *
 * @warning Calling any non-slice methods before calling finalizeSlicedFindPath() or finalizeSlicedFindPathPartial() may result in corrupted data!
 *
 * The @p filter pointer is stored and used for the duration of the sliced path query.
 *
 * @param startRef The reference of the start polygon.
 * @param endRef The reference of the end polygon.
 * @param startPos The position of the start point.
 * @param endPos The position of the end point.
 * @param filter The query filter to apply to the pathfinding.
 * @param options Additional options for the pathfinding query.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::initSlicedFindPath(dtPolyRef startRef, dtPolyRef endRef,
	const float* startPos, const float* endPos,
	const dtQueryFilter* filter, const unsigned int options)
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	// Init path state.
	memset(&m_query, 0, sizeof(dtQueryData));
	m_query.status = DT_FAILURE;
	m_query.startRef = startRef;
	m_query.endRef = endRef;
	dtVcopy(m_query.startPos, startPos);
	dtVcopy(m_query.endPos, endPos);
	m_query.filter = filter;
	m_query.options = options;
	m_query.raycastLimitSqr = FLT_MAX;

	if (!startRef || !endRef)
		return DT_FAILURE | DT_INVALID_PARAM;

	// Validate input
	if (!m_nav->isValidPolyRef(startRef) || !m_nav->isValidPolyRef(endRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	// trade quality with performance?
	if (options & DT_FINDPATH_ANY_ANGLE)
	{
		// limiting to several times the character radius yields nice results. It is not sensitive 
		// so it is enough to compute it from the first tile.
		const dtMeshTile* tile = m_nav->getTileByRef(startRef);
		float agentRadius = tile->header->walkableRadius;
		m_query.raycastLimitSqr = dtSqr(agentRadius * DT_RAY_CAST_LIMIT_PROPORTIONS);
	}

	if (startRef == endRef)
	{
		m_query.status = DT_SUCCESS;
		return DT_SUCCESS;
	}

	m_nodePool->clear();
	m_openList->clear();

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, startPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = dtVdist(startPos, endPos) * H_SCALE;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	m_query.status = DT_IN_PROGRESS;
	m_query.lastBestNode = startNode;
	m_query.lastBestNodeCost = startNode->total;

	return m_query.status;
}

/**
 * @brief Updates a sliced pathfinding query.
 *
 * This function updates an ongoing sliced pathfinding query for a specified maximum number of iterations. It continues the search for the path from the start polygon to the end polygon. It is part of the multi-step process initiated by initSlicedFindPath().
 *
 * @param maxIter The maximum number of iterations to perform during this update.
 * @param doneIters A pointer to store the number of iterations actually performed.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure or completion.
 */
dtStatus dtNavMeshQuery::updateSlicedFindPath(const int maxIter, int* doneIters)
{
	if (!dtStatusInProgress(m_query.status))
		return m_query.status;

	// Make sure the request is still valid.
	if (!m_nav->isValidPolyRef(m_query.startRef) || !m_nav->isValidPolyRef(m_query.endRef))
	{
		m_query.status = DT_FAILURE;
		return DT_FAILURE;
	}

	dtRaycastHit rayHit;
	rayHit.maxPath = 0;

	int iter = 0;
	while (iter < maxIter && !m_openList->empty())
	{
		iter++;

		// Remove node from open list and put it in closed list.
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Reached the goal, stop searching.
		if (bestNode->id == m_query.endRef)
		{
			m_query.lastBestNode = bestNode;
			const dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;
			m_query.status = DT_SUCCESS | details;
			if (doneIters)
				*doneIters = iter;
			return m_query.status;
		}

		// Get current poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		if (dtStatusFailed(m_nav->getTileAndPolyByRef(bestRef, &bestTile, &bestPoly)))
		{
			// The polygon has disappeared during the sliced query, fail.
			m_query.status = DT_FAILURE;
			if (doneIters)
				*doneIters = iter;
			return m_query.status;
		}

		// Get parent and grand parent poly and tile.
		dtPolyRef parentRef = 0, grandpaRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		dtNode* parentNode = 0;
		if (bestNode->pidx)
		{
			parentNode = m_nodePool->getNodeAtIdx(bestNode->pidx);
			parentRef = parentNode->id;
			if (parentNode->pidx)
				grandpaRef = m_nodePool->getNodeAtIdx(parentNode->pidx)->id;
		}
		if (parentRef)
		{
			bool invalidParent = dtStatusFailed(m_nav->getTileAndPolyByRef(parentRef, &parentTile, &parentPoly));
			if (invalidParent || (grandpaRef && !m_nav->isValidPolyRef(grandpaRef)))
			{
				// The polygon has disappeared during the sliced query, fail.
				m_query.status = DT_FAILURE;
				if (doneIters)
					*doneIters = iter;
				return m_query.status;
			}
		}

		// decide whether to test raycast to previous nodes
		bool tryLOS = false;
		if (m_query.options & DT_FINDPATH_ANY_ANGLE)
		{
			if ((parentRef != 0) && (dtVdistSqr(parentNode->pos, bestNode->pos) < m_query.raycastLimitSqr))
				tryLOS = true;
		}

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			dtPolyRef neighbourRef = bestTile->links[i].ref;

			// Skip invalid ids and do not expand back to where we came from.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Get neighbour poly and tile.
			// The API input has been cheked already, skip checking internal data.
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			if (!m_query.filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// get the neighbor node
			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef, 0);
			if (!neighbourNode)
			{
				m_query.status |= DT_OUT_OF_NODES;
				continue;
			}

			// do not expand to nodes that were already visited from the same parent
			if (neighbourNode->pidx != 0 && neighbourNode->pidx == bestNode->pidx)
				continue;

			// If the node is visited the first time, calculate node position.
			if (neighbourNode->flags == 0)
			{
				getEdgeMidPoint(bestRef, bestPoly, bestTile,
					neighbourRef, neighbourPoly, neighbourTile,
					neighbourNode->pos);
			}

			// Calculate cost and heuristic.
			float cost = 0;
			float heuristic = 0;

			// raycast parent
			bool foundShortCut = false;
			rayHit.pathCost = rayHit.t = 0;
			if (tryLOS)
			{
				raycast(parentRef, parentNode->pos, neighbourNode->pos, m_query.filter, DT_RAYCAST_USE_COSTS, &rayHit, grandpaRef);
				foundShortCut = rayHit.t >= 1.0f;
			}

			// update move cost
			if (foundShortCut)
			{
				// shortcut found using raycast. Using shorter cost instead
				cost = parentNode->cost + rayHit.pathCost;
			}
			else
			{
				// No shortcut found.
				const float curCost = m_query.filter->getCost(bestNode->pos, neighbourNode->pos,
					parentRef, parentTile, parentPoly,
					bestRef, bestTile, bestPoly,
					neighbourRef, neighbourTile, neighbourPoly);
				cost = bestNode->cost + curCost;
			}

			// Special case for last node.
			if (neighbourRef == m_query.endRef)
			{
				const float endCost = m_query.filter->getCost(neighbourNode->pos, m_query.endPos,
					bestRef, bestTile, bestPoly,
					neighbourRef, neighbourTile, neighbourPoly,
					0, 0, 0);

				cost = cost + endCost;
				heuristic = 0;
			}
			else
			{
				heuristic = dtVdist(neighbourNode->pos, m_query.endPos) * H_SCALE;
			}

			const float total = cost + heuristic;

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;
			// The node is already visited and process, and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_CLOSED) && total >= neighbourNode->total)
				continue;

			// Add or update the node.
			neighbourNode->pidx = foundShortCut ? bestNode->pidx : m_nodePool->getNodeIdx(bestNode);
			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~(DT_NODE_CLOSED | DT_NODE_PARENT_DETACHED));
			neighbourNode->cost = cost;
			neighbourNode->total = total;
			if (foundShortCut)
				neighbourNode->flags = (neighbourNode->flags | DT_NODE_PARENT_DETACHED);

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				// Already in open, update node location.
				m_openList->modify(neighbourNode);
			}
			else
			{
				// Put the node in open list.
				neighbourNode->flags |= DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}

			// Update nearest node to target so far.
			if (heuristic < m_query.lastBestNodeCost)
			{
				m_query.lastBestNodeCost = heuristic;
				m_query.lastBestNode = neighbourNode;
			}
		}
	}

	// Exhausted all nodes, but could not find path.
	if (m_openList->empty())
	{
		const dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;
		m_query.status = DT_SUCCESS | details;
	}

	if (doneIters)
		*doneIters = iter;

	return m_query.status;
}

/**
 * @brief Finalizes a sliced pathfinding query.
 *
 * This function finalizes an ongoing sliced pathfinding query, retrieving the result path. It is part of the multi-step process initiated by initSlicedFindPath().
 *
 * @param path An array to store the references of the path polygons.
 * @param pathCount A pointer to store the number of path polygons found.
 * @param maxPath The maximum number of polygon references to store in the path array.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, or an error code on failure.
 */
dtStatus dtNavMeshQuery::finalizeSlicedFindPath(dtPolyRef* path, int* pathCount, const int maxPath)
{
	*pathCount = 0;

	if (dtStatusFailed(m_query.status))
	{
		// Reset query.
		memset(&m_query, 0, sizeof(dtQueryData));
		return DT_FAILURE;
	}

	int n = 0;

	if (m_query.startRef == m_query.endRef)
	{
		// Special case: the search starts and ends at same poly.
		path[n++] = m_query.startRef;
	}
	else
	{
		// Reverse the path.
		dtAssert(m_query.lastBestNode);

		if (m_query.lastBestNode->id != m_query.endRef)
			m_query.status |= DT_PARTIAL_RESULT;

		dtNode* prev = 0;
		dtNode* node = m_query.lastBestNode;
		int prevRay = 0;
		do
		{
			dtNode* next = m_nodePool->getNodeAtIdx(node->pidx);
			node->pidx = m_nodePool->getNodeIdx(prev);
			prev = node;
			int nextRay = node->flags & DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
			node->flags = (node->flags & ~DT_NODE_PARENT_DETACHED) | prevRay; // and store it in the reversed path's node
			prevRay = nextRay;
			node = next;
		} while (node);

		// Store path
		node = prev;
		do
		{
			dtNode* next = m_nodePool->getNodeAtIdx(node->pidx);
			dtStatus status = 0;
			if (node->flags & DT_NODE_PARENT_DETACHED)
			{
				float t, normal[3];
				int m;
				status = raycast(node->id, node->pos, next->pos, m_query.filter, &t, normal, path + n, &m, maxPath - n);
				n += m;
				// raycast ends on poly boundary and the path might include the next poly boundary.
				if (path[n - 1] == next->id)
					n--; // remove to avoid duplicates
			}
			else
			{
				path[n++] = node->id;
				if (n >= maxPath)
					status = DT_BUFFER_TOO_SMALL;
			}

			if (status & DT_STATUS_DETAIL_MASK)
			{
				m_query.status |= status & DT_STATUS_DETAIL_MASK;
				break;
			}
			node = next;
		} while (node);
	}

	const dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;

	// Reset query.
	memset(&m_query, 0, sizeof(dtQueryData));

	*pathCount = n;

	return DT_SUCCESS | details;
}

/**
 * @brief Finalizes a sliced pathfinding query with partial results.
 *
 * This function finalizes an ongoing sliced pathfinding query and retrieves a partial result path based on existing nodes and path information. It is part of the multi-step process initiated by initSlicedFindPath() and can be used to extend a path with additional segments.
 *
 * @param existing An array of existing polygon references that represent a partial path.
 * @param existingSize The size of the existing polygon reference array.
 * @param path An array to store the references of the extended path polygons.
 * @param pathCount A pointer to store the number of path polygons found.
 * @param maxPath The maximum number of polygon references to store in the path array.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if the existingSize is zero, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::finalizeSlicedFindPathPartial(const dtPolyRef* existing, const int existingSize,
	dtPolyRef* path, int* pathCount, const int maxPath)
{
	*pathCount = 0;

	if (existingSize == 0)
	{
		return DT_FAILURE;
	}

	if (dtStatusFailed(m_query.status))
	{
		// Reset query.
		memset(&m_query, 0, sizeof(dtQueryData));
		return DT_FAILURE;
	}

	int n = 0;

	if (m_query.startRef == m_query.endRef)
	{
		// Special case: the search starts and ends at same poly.
		path[n++] = m_query.startRef;
	}
	else
	{
		// Find furthest existing node that was visited.
		dtNode* prev = 0;
		dtNode* node = 0;
		for (int i = existingSize - 1; i >= 0; --i)
		{
			m_nodePool->findNodes(existing[i], &node, 1);
			if (node)
				break;
		}

		if (!node)
		{
			m_query.status |= DT_PARTIAL_RESULT;
			dtAssert(m_query.lastBestNode);
			node = m_query.lastBestNode;
		}

		// Reverse the path.
		int prevRay = 0;
		do
		{
			dtNode* next = m_nodePool->getNodeAtIdx(node->pidx);
			node->pidx = m_nodePool->getNodeIdx(prev);
			prev = node;
			int nextRay = node->flags & DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
			node->flags = (node->flags & ~DT_NODE_PARENT_DETACHED) | prevRay; // and store it in the reversed path's node
			prevRay = nextRay;
			node = next;
		} while (node);

		// Store path
		node = prev;
		do
		{
			dtNode* next = m_nodePool->getNodeAtIdx(node->pidx);
			dtStatus status = 0;
			if (node->flags & DT_NODE_PARENT_DETACHED)
			{
				float t, normal[3];
				int m;
				status = raycast(node->id, node->pos, next->pos, m_query.filter, &t, normal, path + n, &m, maxPath - n);
				n += m;
				// raycast ends on poly boundary and the path might include the next poly boundary.
				if (path[n - 1] == next->id)
					n--; // remove to avoid duplicates
			}
			else
			{
				path[n++] = node->id;
				if (n >= maxPath)
					status = DT_BUFFER_TOO_SMALL;
			}

			if (status & DT_STATUS_DETAIL_MASK)
			{
				m_query.status |= status & DT_STATUS_DETAIL_MASK;
				break;
			}
			node = next;
		} while (node);
	}

	const dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;

	// Reset query.
	memset(&m_query, 0, sizeof(dtQueryData));

	*pathCount = n;

	return DT_SUCCESS | details;
}

/**
 * @brief Appends a vertex to a straight path.
 *
 * This function appends a vertex to a straight path, where a straight path is a series of connected vertices representing a path between two points. It is used to extend a straight path during pathfinding.
 *
 * @param pos The position of the vertex to append.
 * @param flags The flags associated with the vertex.
 * @param ref The polygon reference associated with the vertex.
 * @param straightPath An array to store the straight path vertices.
 * @param straightPathFlags An array to store the flags for each vertex in the straight path.
 * @param straightPathRefs An array to store the polygon references for each vertex in the straight path.
 * @param straightPathCount A pointer to store the number of vertices in the straight path.
 * @param maxStraightPath The maximum number of vertices that can be stored in the straight path.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_BUFFER_TOO_SMALL if the maximum path length is exceeded, or DT_IN_PROGRESS if the vertex is appended successfully.
 */
dtStatus dtNavMeshQuery::appendVertex(const float* pos, const unsigned char flags, const dtPolyRef ref,
	float* straightPath, unsigned char* straightPathFlags, dtPolyRef* straightPathRefs,
	int* straightPathCount, const int maxStraightPath) const
{
	if ((*straightPathCount) > 0 && dtVequal(&straightPath[((*straightPathCount) - 1) * 3], pos))
	{
		// The vertices are equal, update flags and poly.
		if (straightPathFlags)
			straightPathFlags[(*straightPathCount) - 1] = flags;
		if (straightPathRefs)
			straightPathRefs[(*straightPathCount) - 1] = ref;
	}
	else
	{
		// Append new vertex.
		dtVcopy(&straightPath[(*straightPathCount) * 3], pos);
		if (straightPathFlags)
			straightPathFlags[(*straightPathCount)] = flags;
		if (straightPathRefs)
			straightPathRefs[(*straightPathCount)] = ref;
		(*straightPathCount)++;
		// If reached end of path or there is no space to append more vertices, return.
		if (flags == DT_STRAIGHTPATH_END || (*straightPathCount) >= maxStraightPath)
		{
			return DT_SUCCESS | (((*straightPathCount) >= maxStraightPath) ? DT_BUFFER_TOO_SMALL : 0);
		}
	}
	return DT_IN_PROGRESS;
}

/**
 * @brief Appends portal vertices to a straight path.
 *
 * This function appends portal vertices to a straight path, extending the path based on portal information. Portals represent connections between polygons in a navigation mesh and are used to create a continuous straight path.
 *
 * @param startIdx The index of the starting vertex in the path.
 * @param endIdx The index of the ending vertex in the path.
 * @param endPos The position of the final destination.
 * @param path An array of polygon references representing the path.
 * @param straightPath An array to store the extended straight path vertices.
 * @param straightPathFlags An array to store the flags for each vertex in the straight path.
 * @param straightPathRefs An array to store the polygon references for each vertex in the straight path.
 * @param straightPathCount A pointer to store the number of vertices in the straight path.
 * @param maxStraightPath The maximum number of vertices that can be stored in the straight path.
 * @param options Additional options for appending the portals.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if the provided references are invalid, or DT_IN_PROGRESS if the portals are appended successfully.
 */
dtStatus dtNavMeshQuery::appendPortals(const int startIdx, const int endIdx, const float* endPos, const dtPolyRef* path,
	float* straightPath, unsigned char* straightPathFlags, dtPolyRef* straightPathRefs,
	int* straightPathCount, const int maxStraightPath, const int options) const
{
	const float* startPos = &straightPath[(*straightPathCount - 1) * 3];
	// Append or update last vertex
	dtStatus stat = 0;
	for (int i = startIdx; i < endIdx; i++)
	{
		// Calculate portal
		const dtPolyRef from = path[i];
		const dtMeshTile* fromTile = 0;
		const dtPoly* fromPoly = 0;
		if (dtStatusFailed(m_nav->getTileAndPolyByRef(from, &fromTile, &fromPoly)))
			return DT_FAILURE | DT_INVALID_PARAM;

		const dtPolyRef to = path[i + 1];
		const dtMeshTile* toTile = 0;
		const dtPoly* toPoly = 0;
		if (dtStatusFailed(m_nav->getTileAndPolyByRef(to, &toTile, &toPoly)))
			return DT_FAILURE | DT_INVALID_PARAM;

		float left[3], right[3];
		if (dtStatusFailed(getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right)))
			break;

		if (options & DT_STRAIGHTPATH_AREA_CROSSINGS)
		{
			// Skip intersection if only area crossings are requested.
			if (fromPoly->getArea() == toPoly->getArea())
				continue;
		}

		// Append intersection
		float s, t;
		if (dtIntersectSegSeg2D(startPos, endPos, left, right, s, t))
		{
			float pt[3];
			dtVlerp(pt, left, right, t);

			stat = appendVertex(pt, 0, path[i + 1],
				straightPath, straightPathFlags, straightPathRefs,
				straightPathCount, maxStraightPath);
			if (stat != DT_IN_PROGRESS)
				return stat;
		}
	}
	return DT_IN_PROGRESS;
}

/**
 * @brief Finds a straight path between two points along a navigation mesh.
 *
 * This method performs 'string pulling,' finding a straight path between two points along a navigation mesh. The start position is clamped to the first polygon in the path, and the end position is clamped to the last polygon. The returned polygon references represent the reference id of the polygon entered at the associated path position, with the reference id associated with the end point always being zero.
 *
 * If the provided result buffers (straightPath, straightPathFlags, straightPathRefs) are too small for the entire result set, they will be filled as far as possible from the start toward the end position.
 *
 * @param startPos The starting position of the path.
 * @param endPos The ending position of the path.
 * @param path An array of polygon references representing the path.
 * @param pathSize The size of the path array.
 * @param straightPath An array to store the vertices of the straight path.
 * @param straightPathFlags An array to store the flags for each vertex in the straight path.
 * @param straightPathRefs An array to store the polygon references for each vertex in the straight path.
 * @param straightPathCount A pointer to store the number of vertices in the straight path.
 * @param maxStraightPath The maximum number of vertices that can be stored in the straight path.
 * @param options Additional options for finding the straight path.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_PARTIAL_RESULT if a partial path is found, DT_BUFFER_TOO_SMALL if the result buffers are too small, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::findStraightPath(const float* startPos, const float* endPos,
	const dtPolyRef* path, const int pathSize,
	float* straightPath, unsigned char* straightPathFlags, dtPolyRef* straightPathRefs,
	int* straightPathCount, const int maxStraightPath, const int options) const
{
	dtAssert(m_nav);

	*straightPathCount = 0;

	if (!maxStraightPath)
		return DT_FAILURE | DT_INVALID_PARAM;

	if (!path[0])
		return DT_FAILURE | DT_INVALID_PARAM;

	dtStatus stat = 0;

	// TODO: Should this be callers responsibility?
	float closestStartPos[3];
	if (dtStatusFailed(closestPointOnPolyBoundary(path[0], startPos, closestStartPos)))
		return DT_FAILURE | DT_INVALID_PARAM;

	float closestEndPos[3];
	if (dtStatusFailed(closestPointOnPolyBoundary(path[pathSize - 1], endPos, closestEndPos)))
		return DT_FAILURE | DT_INVALID_PARAM;

	// Add start point.
	stat = appendVertex(closestStartPos, DT_STRAIGHTPATH_START, path[0],
		straightPath, straightPathFlags, straightPathRefs,
		straightPathCount, maxStraightPath);
	if (stat != DT_IN_PROGRESS)
		return stat;

	if (pathSize > 1)
	{
		float portalApex[3], portalLeft[3], portalRight[3];
		dtVcopy(portalApex, closestStartPos);
		dtVcopy(portalLeft, portalApex);
		dtVcopy(portalRight, portalApex);
		int apexIndex = 0;
		int leftIndex = 0;
		int rightIndex = 0;

		unsigned char leftPolyType = 0;
		unsigned char rightPolyType = 0;

		dtPolyRef leftPolyRef = path[0];
		dtPolyRef rightPolyRef = path[0];

		for (int i = 0; i < pathSize; ++i)
		{
			float left[3], right[3];
			unsigned char fromType, toType;

			if (i + 1 < pathSize)
			{
				// Next portal.
				if (dtStatusFailed(getPortalPoints(path[i], path[i + 1], left, right, fromType, toType)))
				{
					// Failed to get portal points, in practice this means that path[i+1] is invalid polygon.
					// Clamp the end point to path[i], and return the path so far.

					if (dtStatusFailed(closestPointOnPolyBoundary(path[i], endPos, closestEndPos)))
					{
						// This should only happen when the first polygon is invalid.
						return DT_FAILURE | DT_INVALID_PARAM;
					}

					// Apeend portals along the current straight path segment.
					if (options & (DT_STRAIGHTPATH_AREA_CROSSINGS | DT_STRAIGHTPATH_ALL_CROSSINGS))
					{
						stat = appendPortals(apexIndex, i, closestEndPos, path,
							straightPath, straightPathFlags, straightPathRefs,
							straightPathCount, maxStraightPath, options);
					}

					stat = appendVertex(closestEndPos, 0, path[i],
						straightPath, straightPathFlags, straightPathRefs,
						straightPathCount, maxStraightPath);

					return DT_SUCCESS | DT_PARTIAL_RESULT | ((*straightPathCount >= maxStraightPath) ? DT_BUFFER_TOO_SMALL : 0);
				}

				// If starting really close the portal, advance.
				if (i == 0)
				{
					float t;
					if (dtDistancePtSegSqr2D(portalApex, left, right, t) < dtSqr(0.001f))
						continue;
				}
			}
			else
			{
				// End of the path.
				dtVcopy(left, closestEndPos);
				dtVcopy(right, closestEndPos);

				fromType = toType = DT_POLYTYPE_GROUND;
			}

			// Right vertex.
			if (dtTriArea2D(portalApex, portalRight, right) <= 0.0f)
			{
				if (dtVequal(portalApex, portalRight) || dtTriArea2D(portalApex, portalLeft, right) > 0.0f)
				{
					dtVcopy(portalRight, right);
					rightPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
					rightPolyType = toType;
					rightIndex = i;
				}
				else
				{
					// Append portals along the current straight path segment.
					if (options & (DT_STRAIGHTPATH_AREA_CROSSINGS | DT_STRAIGHTPATH_ALL_CROSSINGS))
					{
						stat = appendPortals(apexIndex, leftIndex, portalLeft, path,
							straightPath, straightPathFlags, straightPathRefs,
							straightPathCount, maxStraightPath, options);
						if (stat != DT_IN_PROGRESS)
							return stat;
					}

					dtVcopy(portalApex, portalLeft);
					apexIndex = leftIndex;

					unsigned char flags = 0;
					if (!leftPolyRef)
						flags = DT_STRAIGHTPATH_END;
					else if (leftPolyType == DT_POLYTYPE_OFFMESH_CONNECTION)
						flags = DT_STRAIGHTPATH_OFFMESH_CONNECTION;
					dtPolyRef ref = leftPolyRef;

					// Append or update vertex
					stat = appendVertex(portalApex, flags, ref,
						straightPath, straightPathFlags, straightPathRefs,
						straightPathCount, maxStraightPath);
					if (stat != DT_IN_PROGRESS)
						return stat;

					dtVcopy(portalLeft, portalApex);
					dtVcopy(portalRight, portalApex);
					leftIndex = apexIndex;
					rightIndex = apexIndex;

					// Restart
					i = apexIndex;

					continue;
				}
			}

			// Left vertex.
			if (dtTriArea2D(portalApex, portalLeft, left) >= 0.0f)
			{
				if (dtVequal(portalApex, portalLeft) || dtTriArea2D(portalApex, portalRight, left) < 0.0f)
				{
					dtVcopy(portalLeft, left);
					leftPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
					leftPolyType = toType;
					leftIndex = i;
				}
				else
				{
					// Append portals along the current straight path segment.
					if (options & (DT_STRAIGHTPATH_AREA_CROSSINGS | DT_STRAIGHTPATH_ALL_CROSSINGS))
					{
						stat = appendPortals(apexIndex, rightIndex, portalRight, path,
							straightPath, straightPathFlags, straightPathRefs,
							straightPathCount, maxStraightPath, options);
						if (stat != DT_IN_PROGRESS)
							return stat;
					}

					dtVcopy(portalApex, portalRight);
					apexIndex = rightIndex;

					unsigned char flags = 0;
					if (!rightPolyRef)
						flags = DT_STRAIGHTPATH_END;
					else if (rightPolyType == DT_POLYTYPE_OFFMESH_CONNECTION)
						flags = DT_STRAIGHTPATH_OFFMESH_CONNECTION;
					dtPolyRef ref = rightPolyRef;

					// Append or update vertex
					stat = appendVertex(portalApex, flags, ref,
						straightPath, straightPathFlags, straightPathRefs,
						straightPathCount, maxStraightPath);
					if (stat != DT_IN_PROGRESS)
						return stat;

					dtVcopy(portalLeft, portalApex);
					dtVcopy(portalRight, portalApex);
					leftIndex = apexIndex;
					rightIndex = apexIndex;

					// Restart
					i = apexIndex;

					continue;
				}
			}
		}

		// Append portals along the current straight path segment.
		if (options & (DT_STRAIGHTPATH_AREA_CROSSINGS | DT_STRAIGHTPATH_ALL_CROSSINGS))
		{
			stat = appendPortals(apexIndex, pathSize - 1, closestEndPos, path,
				straightPath, straightPathFlags, straightPathRefs,
				straightPathCount, maxStraightPath, options);
			if (stat != DT_IN_PROGRESS)
				return stat;
		}
	}

	stat = appendVertex(closestEndPos, DT_STRAIGHTPATH_END, 0,
		straightPath, straightPathFlags, straightPathRefs,
		straightPathCount, maxStraightPath);

	return DT_SUCCESS | ((*straightPathCount >= maxStraightPath) ? DT_BUFFER_TOO_SMALL : 0);
}

/**
 * @brief Moves along the surface of a navigation mesh from a start position to an end position.
 *
 * This method moves along the surface of a navigation mesh from a start position to an end position, avoiding obstacles and returning the closest reachable position if the end is not reached. The result position is not projected onto the surface of the navigation mesh.
 *
 * @param startRef The polygon reference representing the starting position.
 * @param startPos The starting position in world coordinates.
 * @param endPos The ending position in world coordinates.
 * @param filter The query filter to control which polygons can be traversed.
 * @param resultPos The result position, which will be the end position if reached, or the closest reachable position otherwise.
 * @param visited An array to store the visited polygon references along the path.
 * @param visitedCount A pointer to store the number of visited polygons.
 * @param maxVisitedSize The maximum number of polygon references that can be stored in the visited array.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if there is an invalid parameter, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::moveAlongSurface(dtPolyRef startRef, const float* startPos, const float* endPos,
	const dtQueryFilter* filter,
	float* resultPos, dtPolyRef* visited, int* visitedCount, const int maxVisitedSize) const
{
	dtAssert(m_nav);
	dtAssert(m_tinyNodePool);

	*visitedCount = 0;

	// Validate input
	if (!startRef)
		return DT_FAILURE | DT_INVALID_PARAM;
	if (!m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	dtStatus status = DT_SUCCESS;

	static const int MAX_STACK = 48;
	dtNode* stack[MAX_STACK];
	int nstack = 0;

	m_tinyNodePool->clear();

	dtNode* startNode = m_tinyNodePool->getNode(startRef);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_CLOSED;
	stack[nstack++] = startNode;

	float bestPos[3];
	float bestDist = FLT_MAX;
	dtNode* bestNode = 0;
	dtVcopy(bestPos, startPos);

	// Search constraints
	float searchPos[3], searchRadSqr;
	dtVlerp(searchPos, startPos, endPos, 0.5f);
	searchRadSqr = dtSqr(dtVdist(startPos, endPos) / 2.0f + 0.001f);

	float verts[DT_VERTS_PER_POLYGON * 3];

	while (nstack)
	{
		// Pop front.
		dtNode* curNode = stack[0];
		for (int i = 0; i < nstack - 1; ++i)
			stack[i] = stack[i + 1];
		nstack--;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef curRef = curNode->id;
		const dtMeshTile* curTile = 0;
		const dtPoly* curPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(curRef, &curTile, &curPoly);

		// Collect vertices.
		const int nverts = curPoly->vertCount;
		for (int i = 0; i < nverts; ++i)
			dtVcopy(&verts[i * 3], &curTile->verts[curPoly->verts[i] * 3]);

		// If target is inside the poly, stop search.
		if (dtPointInPolygon(endPos, verts, nverts))
		{
			bestNode = curNode;
			dtVcopy(bestPos, endPos);
			break;
		}

		// Find wall edges and find nearest point inside the walls.
		for (int i = 0, j = (int)curPoly->vertCount - 1; i < (int)curPoly->vertCount; j = i++)
		{
			// Find links to neighbours.
			static const int MAX_NEIS = 8;
			int nneis = 0;
			dtPolyRef neis[MAX_NEIS];

			if (curPoly->neis[j] & DT_EXT_LINK)
			{
				// Tile border.
				for (unsigned int k = curPoly->firstLink; k != DT_NULL_LINK; k = curTile->links[k].next)
				{
					const dtLink* link = &curTile->links[k];
					if (link->edge == j)
					{
						if (link->ref != 0)
						{
							const dtMeshTile* neiTile = 0;
							const dtPoly* neiPoly = 0;
							m_nav->getTileAndPolyByRefUnsafe(link->ref, &neiTile, &neiPoly);
							if (filter->passFilter(link->ref, neiTile, neiPoly))
							{
								if (nneis < MAX_NEIS)
									neis[nneis++] = link->ref;
							}
						}
					}
				}
			}
			else if (curPoly->neis[j])
			{
				const unsigned int idx = (unsigned int)(curPoly->neis[j] - 1);
				const dtPolyRef ref = m_nav->getPolyRefBase(curTile) | idx;
				if (filter->passFilter(ref, curTile, &curTile->polys[idx]))
				{
					// Internal edge, encode id.
					neis[nneis++] = ref;
				}
			}

			if (!nneis)
			{
				// Wall edge, calc distance.
				const float* vj = &verts[j * 3];
				const float* vi = &verts[i * 3];
				float tseg;
				const float distSqr = dtDistancePtSegSqr2D(endPos, vj, vi, tseg);
				if (distSqr < bestDist)
				{
					// Update nearest distance.
					dtVlerp(bestPos, vj, vi, tseg);
					bestDist = distSqr;
					bestNode = curNode;
				}
			}
			else
			{
				for (int k = 0; k < nneis; ++k)
				{
					// Skip if no node can be allocated.
					dtNode* neighbourNode = m_tinyNodePool->getNode(neis[k]);
					if (!neighbourNode)
						continue;
					// Skip if already visited.
					if (neighbourNode->flags & DT_NODE_CLOSED)
						continue;

					// Skip the link if it is too far from search constraint.
					// TODO: Maybe should use getPortalPoints(), but this one is way faster.
					const float* vj = &verts[j * 3];
					const float* vi = &verts[i * 3];
					float tseg;
					float distSqr = dtDistancePtSegSqr2D(searchPos, vj, vi, tseg);
					if (distSqr > searchRadSqr)
						continue;

					// Mark as the node as visited and push to queue.
					if (nstack < MAX_STACK)
					{
						neighbourNode->pidx = m_tinyNodePool->getNodeIdx(curNode);
						neighbourNode->flags |= DT_NODE_CLOSED;
						stack[nstack++] = neighbourNode;
					}
				}
			}
		}
	}

	int n = 0;
	if (bestNode)
	{
		// Reverse the path.
		dtNode* prev = 0;
		dtNode* node = bestNode;
		do
		{
			dtNode* next = m_tinyNodePool->getNodeAtIdx(node->pidx);
			node->pidx = m_tinyNodePool->getNodeIdx(prev);
			prev = node;
			node = next;
		} while (node);

		// Store result
		node = prev;
		do
		{
			visited[n++] = node->id;
			if (n >= maxVisitedSize)
			{
				status |= DT_BUFFER_TOO_SMALL;
				break;
			}
			node = m_tinyNodePool->getNodeAtIdx(node->pidx);
		} while (node);
	}

	dtVcopy(resultPos, bestPos);

	*visitedCount = n;

	return status;
}

/**
 * @brief Gets portal points between two polygons.
 *
 * This method retrieves the portal points between two polygons specified by their references. The portal points define the left and right boundaries of the path segment between the two polygons.
 *
 * @param from The reference of the starting polygon.
 * @param to The reference of the ending polygon.
 * @param left An array to store the left boundary of the portal.
 * @param right An array to store the right boundary of the portal.
 * @param fromType The type of the starting polygon.
 * @param toType The type of the ending polygon.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if there is an invalid parameter, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::getPortalPoints(dtPolyRef from, dtPolyRef to, float* left, float* right,
	unsigned char& fromType, unsigned char& toType) const
{
	dtAssert(m_nav);

	const dtMeshTile* fromTile = 0;
	const dtPoly* fromPoly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(from, &fromTile, &fromPoly)))
		return DT_FAILURE | DT_INVALID_PARAM;
	fromType = fromPoly->getType();

	const dtMeshTile* toTile = 0;
	const dtPoly* toPoly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(to, &toTile, &toPoly)))
		return DT_FAILURE | DT_INVALID_PARAM;
	toType = toPoly->getType();

	return getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right);
}

/**
 * @brief Returns portal points between two polygons.
 *
 * This method retrieves the portal points between two polygons specified by their references. The portal points define the left and right boundaries of the path segment between the two polygons.
 *
 * @param from The reference of the starting polygon.
 * @param fromPoly The pointer to the starting polygon's data.
 * @param fromTile The pointer to the tile containing the starting polygon.
 * @param to The reference of the ending polygon.
 * @param toPoly The pointer to the ending polygon's data.
 * @param toTile The pointer to the tile containing the ending polygon.
 * @param left An array to store the left boundary of the portal.
 * @param right An array to store the right boundary of the portal.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if there is an invalid parameter, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::getPortalPoints(dtPolyRef from, const dtPoly* fromPoly, const dtMeshTile* fromTile,
	dtPolyRef to, const dtPoly* toPoly, const dtMeshTile* toTile,
	float* left, float* right) const
{
	// Find the link that points to the 'to' polygon.
	const dtLink* link = 0;
	for (unsigned int i = fromPoly->firstLink; i != DT_NULL_LINK; i = fromTile->links[i].next)
	{
		if (fromTile->links[i].ref == to)
		{
			link = &fromTile->links[i];
			break;
		}
	}
	if (!link)
		return DT_FAILURE | DT_INVALID_PARAM;

	// Handle off-mesh connections.
	if (fromPoly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
	{
		// Find link that points to first vertex.
		for (unsigned int i = fromPoly->firstLink; i != DT_NULL_LINK; i = fromTile->links[i].next)
		{
			if (fromTile->links[i].ref == to)
			{
				const int v = fromTile->links[i].edge;
				dtVcopy(left, &fromTile->verts[fromPoly->verts[v] * 3]);
				dtVcopy(right, &fromTile->verts[fromPoly->verts[v] * 3]);
				return DT_SUCCESS;
			}
		}
		return DT_FAILURE | DT_INVALID_PARAM;
	}

	if (toPoly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
	{
		for (unsigned int i = toPoly->firstLink; i != DT_NULL_LINK; i = toTile->links[i].next)
		{
			if (toTile->links[i].ref == from)
			{
				const int v = toTile->links[i].edge;
				dtVcopy(left, &toTile->verts[toPoly->verts[v] * 3]);
				dtVcopy(right, &toTile->verts[toPoly->verts[v] * 3]);
				return DT_SUCCESS;
			}
		}
		return DT_FAILURE | DT_INVALID_PARAM;
	}

	// Find portal vertices.
	const int v0 = fromPoly->verts[link->edge];
	const int v1 = fromPoly->verts[(link->edge + 1) % (int)fromPoly->vertCount];
	dtVcopy(left, &fromTile->verts[v0 * 3]);
	dtVcopy(right, &fromTile->verts[v1 * 3]);

	// If the link is at tile boundary, dtClamp the vertices to
	// the link width.
	if (link->side != 0xff)
	{
		// Unpack portal limits.
		if (link->bmin != 0 || link->bmax != 255)
		{
			const float s = 1.0f / 255.0f;
			const float tmin = link->bmin * s;
			const float tmax = link->bmax * s;
			dtVlerp(left, &fromTile->verts[v0 * 3], &fromTile->verts[v1 * 3], tmin);
			dtVlerp(right, &fromTile->verts[v0 * 3], &fromTile->verts[v1 * 3], tmax);
		}
	}

	return DT_SUCCESS;
}

/**
 * @brief Returns the edge midpoint between two polygons.
 *
 * This method calculates the midpoint on the edge between two polygons specified by their references. The midpoint is computed as the average position between the portal points of the two polygons.
 *
 * @param from The reference of the starting polygon.
 * @param to The reference of the ending polygon.
 * @param mid An array to store the computed midpoint.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if there is an invalid parameter, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::getEdgeMidPoint(dtPolyRef from, dtPolyRef to, float* mid) const
{
	float left[3], right[3];
	unsigned char fromType, toType;
	if (dtStatusFailed(getPortalPoints(from, to, left, right, fromType, toType)))
		return DT_FAILURE | DT_INVALID_PARAM;
	mid[0] = (left[0] + right[0]) * 0.5f;
	mid[1] = (left[1] + right[1]) * 0.5f;
	mid[2] = (left[2] + right[2]) * 0.5f;
	return DT_SUCCESS;
}

/**
 * @brief Returns the edge midpoint between two polygons.
 *
 * This method calculates the midpoint on the edge between two polygons specified by their references. The midpoint is computed as the average position between the portal points of the two polygons.
 *
 * @param from The reference of the starting polygon.
 * @param fromPoly The pointer to the starting polygon's data.
 * @param fromTile The pointer to the tile containing the starting polygon.
 * @param to The reference of the ending polygon.
 * @param toPoly The pointer to the ending polygon's data.
 * @param toTile The pointer to the tile containing the ending polygon.
 * @param mid An array to store the computed midpoint.
 *
 * @return The status of the operation. Returns DT_SUCCESS on success, DT_FAILURE if there is an invalid parameter, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::getEdgeMidPoint(dtPolyRef from, const dtPoly* fromPoly, const dtMeshTile* fromTile,
	dtPolyRef to, const dtPoly* toPoly, const dtMeshTile* toTile,
	float* mid) const
{
	float left[3], right[3];
	if (dtStatusFailed(getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right)))
		return DT_FAILURE | DT_INVALID_PARAM;
	mid[0] = (left[0] + right[0]) * 0.5f;
	mid[1] = (left[1] + right[1]) * 0.5f;
	mid[2] = (left[2] + right[2]) * 0.5f;
	return DT_SUCCESS;
}

/**
 * @brief Performs a raycast query from the start position to the end position on the navigation mesh.
 *
 * This method is meant to be used for quick, short-distance checks. It performs a raycast query from the start position to the end position along the navigation mesh to determine if there are any obstacles or walls in the way.
 *
 * If the path array is too small to hold the result, it will be filled as far as possible from the start position toward the end position.
 *
 * @param startRef The reference of the starting polygon.
 * @param startPos The 3D coordinates of the starting position.
 * @param endPos The 3D coordinates of the ending position.
 * @param filter The query filter that specifies which polygons can be traversed.
 * @param t The hit parameter representing the point of intersection between the ray and the navigation mesh.
 * @param hitNormal The hit normal vector at the point of intersection (optional).
 * @param path An array to store the visited polygon references along the ray path (optional).
 * @param pathCount The number of visited polygons in the path (optional).
 * @param maxPath The maximum number of polygons that can be stored in the path array.
 *
 * @return The status of the operation. Returns DT_SUCCESS if the ray hit the navigation mesh, DT_FAILURE if there is an invalid parameter, DT_SUCCESS_NO_RESULT if the ray reaches the end position without hitting anything, or an error code on other failures.
 *
 * @par Using the Hit Parameter (t):
 * If the hit parameter is a very high value (FLT_MAX), then the ray has hit the end position. In this case, the path represents a valid corridor to the end position, and the value of @p hitNormal is undefined.
 *
 * If the hit parameter is zero, then the start position is on the wall that was hit, and the value of @p hitNormal is undefined.
 *
 * If 0 < t < 1.0, then the following applies:
 *
 * @code
 * distanceToHitBorder = distanceToEndPosition * t
 * hitPoint = startPos + (endPos - startPos) * t
 * @endcode
 *
 * @par Use Case Restriction:
 * The raycast ignores the y-value of the end position (2D check). This places significant limits on how it can be used. For example, if there is a main floor with a second-floor balcony that hangs over the main floor, the raycast will search toward the end position along the first-floor mesh. If it reaches the end position's xz-coordinates, it will indicate FLT_MAX (no wall hit), meaning it reached the end position. This method is intended for short-distance checks.
 */
dtStatus dtNavMeshQuery::raycast(dtPolyRef startRef, const float* startPos, const float* endPos,
	const dtQueryFilter* filter,
	float* t, float* hitNormal, dtPolyRef* path, int* pathCount, const int maxPath) const
{
	dtRaycastHit hit;
	hit.path = path;
	hit.maxPath = maxPath;

	dtStatus status = raycast(startRef, startPos, endPos, filter, 0, &hit);

	*t = hit.t;
	if (hitNormal)
		dtVcopy(hitNormal, hit.hitNormal);
	if (pathCount)
		*pathCount = hit.pathCount;

	return status;
}

/**
 * @brief Performs a raycast query from the start position to the end position on the navigation mesh.
 *
 * This method is meant to be used for quick, short-distance checks. It performs a raycast query from the start position to the end position along the navigation mesh to determine if there are any obstacles or walls in the way.
 *
 * If the path array is too small to hold the result, it will be filled as far as possible from the start position toward the end position.
 *
 * @param startRef The reference of the starting polygon.
 * @param startPos The 3D coordinates of the starting position.
 * @param endPos The 3D coordinates of the ending position.
 * @param filter The query filter that specifies which polygons can be traversed.
 * @param options Additional options for the raycast query.
 * @param hit A raycast hit result that will be filled with the query results.
 * @param prevRef The reference of the previous polygon (optional).
 *
 * @return The status of the operation. Returns DT_SUCCESS if the ray hit the navigation mesh, DT_FAILURE if there is an invalid parameter, DT_SUCCESS_NO_RESULT if the ray reaches the end position without hitting anything, or an error code on other failures.
 *
 * @par Using the Hit Parameter (t):
 * If the hit parameter is a very high value (FLT_MAX), then the ray has hit the end position. In this case, the path represents a valid corridor to the end position, and the value of @p hitNormal is undefined.
 *
 * If the hit parameter is zero, then the start position is on the wall that was hit, and the value of @p hitNormal is undefined.
 *
 * If 0 < t < 1.0, then the following applies:
 *
 * @code
 * distanceToHitBorder = distanceToEndPosition * t
 * hitPoint = startPos + (endPos - startPos) * t
 * @endcode
 *
 * @par Use Case Restriction:
 * The raycast ignores the y-value of the end position (2D check). This places significant limits on how it can be used. For example, if there is a main floor with a second-floor balcony that hangs over the main floor, the raycast will search toward the end position along the first-floor mesh. If it reaches the end position's xz-coordinates, it will indicate FLT_MAX (no wall hit), meaning it reached the end position. This method is intended for short-distance checks.
 */
dtStatus dtNavMeshQuery::raycast(dtPolyRef startRef, const float* startPos, const float* endPos,
	const dtQueryFilter* filter, const unsigned int options,
	dtRaycastHit* hit, dtPolyRef prevRef) const
{
	dtAssert(m_nav);

	hit->t = 0;
	hit->pathCount = 0;
	hit->pathCost = 0;

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;
	if (prevRef && !m_nav->isValidPolyRef(prevRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	float dir[3], curPos[3], lastPos[3];
	float verts[DT_VERTS_PER_POLYGON * 3 + 3];
	int n = 0;

	dtVcopy(curPos, startPos);
	dtVsub(dir, endPos, startPos);
	dtVset(hit->hitNormal, 0, 0, 0);

	dtStatus status = DT_SUCCESS;

	const dtMeshTile* prevTile, * tile, * nextTile;
	const dtPoly* prevPoly, * poly, * nextPoly;
	dtPolyRef curRef, nextRef;

	// The API input has been checked already, skip checking internal data.
	nextRef = curRef = startRef;
	tile = 0;
	poly = 0;
	m_nav->getTileAndPolyByRefUnsafe(curRef, &tile, &poly);
	nextTile = prevTile = tile;
	nextPoly = prevPoly = poly;
	if (prevRef)
		m_nav->getTileAndPolyByRefUnsafe(prevRef, &prevTile, &prevPoly);

	while (curRef)
	{
		// Cast ray against current polygon.

		// Collect vertices.
		int nv = 0;
		for (int i = 0; i < (int)poly->vertCount; ++i)
		{
			dtVcopy(&verts[nv * 3], &tile->verts[poly->verts[i] * 3]);
			nv++;
		}

		float tmin, tmax;
		int segMin, segMax;
		if (!dtIntersectSegmentPoly2D(startPos, endPos, verts, nv, tmin, tmax, segMin, segMax))
		{
			// Could not hit the polygon, keep the old t and report hit.
			hit->pathCount = n;
			return status;
		}
		// Keep track of furthest t so far.
		if (tmax > hit->t)
			hit->t = tmax;

		// Store visited polygons.
		if (n < hit->maxPath)
			hit->path[n++] = curRef;
		else
			status |= DT_BUFFER_TOO_SMALL;

		// Ray end is completely inside the polygon.
		if (segMax == -1)
		{
			hit->t = FLT_MAX;
			hit->pathCount = n;

			// add the cost
			if (options & DT_RAYCAST_USE_COSTS)
				hit->pathCost += filter->getCost(curPos, endPos, prevRef, prevTile, prevPoly, curRef, tile, poly, curRef, tile, poly);
			return status;
		}

		// Follow neighbours.
		nextRef = 0;

		for (unsigned int i = poly->firstLink; i != DT_NULL_LINK; i = tile->links[i].next)
		{
			const dtLink* link = &tile->links[i];

			// Find link which contains this edge.
			if ((int)link->edge != segMax)
				continue;

			// Get pointer to the next polygon.
			nextTile = 0;
			nextPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(link->ref, &nextTile, &nextPoly);

			// Skip off-mesh connections.
			if (nextPoly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
				continue;

			// Skip links based on filter.
			if (!filter->passFilter(link->ref, nextTile, nextPoly))
				continue;

			// If the link is internal, just return the ref.
			if (link->side == 0xff)
			{
				nextRef = link->ref;
				break;
			}

			// If the link is at tile boundary,

			// Check if the link spans the whole edge, and accept.
			if (link->bmin == 0 && link->bmax == 255)
			{
				nextRef = link->ref;
				break;
			}

			// Check for partial edge links.
			const int v0 = poly->verts[link->edge];
			const int v1 = poly->verts[(link->edge + 1) % poly->vertCount];
			const float* left = &tile->verts[v0 * 3];
			const float* right = &tile->verts[v1 * 3];

			// Check that the intersection lies inside the link portal.
			if (link->side == 0 || link->side == 4)
			{
				// Calculate link size.
				const float s = 1.0f / 255.0f;
				float lmin = left[2] + (right[2] - left[2]) * (link->bmin * s);
				float lmax = left[2] + (right[2] - left[2]) * (link->bmax * s);
				if (lmin > lmax) dtSwap(lmin, lmax);

				// Find Z intersection.
				float z = startPos[2] + (endPos[2] - startPos[2]) * tmax;
				if (z >= lmin && z <= lmax)
				{
					nextRef = link->ref;
					break;
				}
			}
			else if (link->side == 2 || link->side == 6)
			{
				// Calculate link size.
				const float s = 1.0f / 255.0f;
				float lmin = left[0] + (right[0] - left[0]) * (link->bmin * s);
				float lmax = left[0] + (right[0] - left[0]) * (link->bmax * s);
				if (lmin > lmax) dtSwap(lmin, lmax);

				// Find X intersection.
				float x = startPos[0] + (endPos[0] - startPos[0]) * tmax;
				if (x >= lmin && x <= lmax)
				{
					nextRef = link->ref;
					break;
				}
			}
		}

		// add the cost
		if (options & DT_RAYCAST_USE_COSTS)
		{
			// compute the intersection point at the furthest end of the polygon
			// and correct the height (since the raycast moves in 2d)
			dtVcopy(lastPos, curPos);
			dtVmad(curPos, startPos, dir, hit->t);
			float* e1 = &verts[segMax * 3];
			float* e2 = &verts[((segMax + 1) % nv) * 3];
			float eDir[3], diff[3];
			dtVsub(eDir, e2, e1);
			dtVsub(diff, curPos, e1);
			float s = dtSqr(eDir[0]) > dtSqr(eDir[2]) ? diff[0] / eDir[0] : diff[2] / eDir[2];
			curPos[1] = e1[1] + eDir[1] * s;

			hit->pathCost += filter->getCost(lastPos, curPos, prevRef, prevTile, prevPoly, curRef, tile, poly, nextRef, nextTile, nextPoly);
		}

		if (!nextRef)
		{
			// No neighbour, we hit a wall.

			// Calculate hit normal.
			const int a = segMax;
			const int b = segMax + 1 < nv ? segMax + 1 : 0;
			const float* va = &verts[a * 3];
			const float* vb = &verts[b * 3];
			const float dx = vb[0] - va[0];
			const float dz = vb[2] - va[2];
			hit->hitNormal[0] = dz;
			hit->hitNormal[1] = 0;
			hit->hitNormal[2] = -dx;
			dtVnormalize(hit->hitNormal);

			hit->pathCount = n;
			return status;
		}

		// No hit, advance to neighbour polygon.
		prevRef = curRef;
		curRef = nextRef;
		prevTile = tile;
		tile = nextTile;
		prevPoly = poly;
		poly = nextPoly;
	}

	hit->pathCount = n;

	return status;
}

/**
 * @brief Finds polygons within a circle on the navigation mesh.
 *
 * This method is used to find polygons within a specified radius from a center point on the navigation mesh. The order of the result set is from the least to the highest cost to reach the polygons.
 *
 * At least one result array must be provided. If the result arrays are too small to hold the entire result set, they will be filled to capacity.
 *
 * A common use case for this method is to perform Dijkstra searches. Candidate polygons are found by searching the graph beginning at the start polygon.
 *
 * If a polygon is not found via the graph search, even if it intersects the search circle, it will not be included in the result set. For example:
 * - polyA is the start polygon.
 * - polyB shares an edge with polyA (is adjacent).
 * - polyC shares an edge with polyB but not with polyA.
 * Even if the search circle overlaps polyC, it will not be included in the result set unless polyB is also in the set.
 *
 * The value of the center point is used as the start position for cost calculations. It is not projected onto the surface of the mesh, so its y-value will affect the costs.
 *
 * Intersection tests occur in 2D. All polygons and the search circle are projected onto the xz-plane, so the y-value of the center point does not affect intersection tests.
 *
 * @param startRef The reference of the starting polygon.
 * @param centerPos The 3D coordinates of the center point of the search circle.
 * @param radius The radius of the search circle.
 * @param filter The query filter that specifies which polygons can be traversed.
 * @param resultRef An array to store the polygon references of the found polygons.
 * @param resultParent An array to store the parent polygon references corresponding to the found polygons.
 * @param resultCost An array to store the costs to reach the found polygons.
 * @param resultCount A pointer to the variable that will hold the number of found polygons.
 * @param maxResult The maximum number of polygons that can be stored in the result arrays.
 *
 * @return The status of the operation. Returns DT_SUCCESS if polygons are found within the circle, DT_FAILURE if there is an invalid parameter, DT_OUT_OF_NODES if the operation runs out of nodes, DT_BUFFER_TOO_SMALL if the result arrays are too small to hold the entire result set, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::findPolysAroundCircle(dtPolyRef startRef, const float* centerPos, const float radius,
	const dtQueryFilter* filter,
	dtPolyRef* resultRef, dtPolyRef* resultParent, float* resultCost,
	int* resultCount, const int maxResult) const
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	*resultCount = 0;

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	m_nodePool->clear();
	m_openList->clear();

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, centerPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	dtStatus status = DT_SUCCESS;

	int n = 0;
	if (n < maxResult)
	{
		if (resultRef)
			resultRef[n] = startNode->id;
		if (resultParent)
			resultParent[n] = 0;
		if (resultCost)
			resultCost[n] = 0;
		++n;
	}
	else
	{
		status |= DT_BUFFER_TOO_SMALL;
	}

	const float radiusSqr = dtSqr(radius);

	while (!m_openList->empty())
	{
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(bestRef, &bestTile, &bestPoly);

		// Get parent poly and tile.
		dtPolyRef parentRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		if (bestNode->pidx)
			parentRef = m_nodePool->getNodeAtIdx(bestNode->pidx)->id;
		if (parentRef)
			m_nav->getTileAndPolyByRefUnsafe(parentRef, &parentTile, &parentPoly);

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			const dtLink* link = &bestTile->links[i];
			dtPolyRef neighbourRef = link->ref;
			// Skip invalid neighbours and do not follow back to parent.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Expand to neighbour
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			// Do not advance if the polygon is excluded by the filter.
			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// Find edge and calc distance to the edge.
			float va[3], vb[3];
			if (!getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb))
				continue;

			// If the circle is not touching the next polygon, skip it.
			float tseg;
			float distSqr = dtDistancePtSegSqr2D(centerPos, va, vb, tseg);
			if (distSqr > radiusSqr)
				continue;

			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef);
			if (!neighbourNode)
			{
				status |= DT_OUT_OF_NODES;
				continue;
			}

			if (neighbourNode->flags & DT_NODE_CLOSED)
				continue;

			// Cost
			if (neighbourNode->flags == 0)
				dtVlerp(neighbourNode->pos, va, vb, 0.5f);

			const float total = bestNode->total + dtVdist(bestNode->pos, neighbourNode->pos);

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;

			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~DT_NODE_CLOSED);
			neighbourNode->pidx = m_nodePool->getNodeIdx(bestNode);
			neighbourNode->total = total;

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				m_openList->modify(neighbourNode);
			}
			else
			{
				if (n < maxResult)
				{
					if (resultRef)
						resultRef[n] = neighbourNode->id;
					if (resultParent)
						resultParent[n] = m_nodePool->getNodeAtIdx(neighbourNode->pidx)->id;
					if (resultCost)
						resultCost[n] = neighbourNode->total;
					++n;
				}
				else
				{
					status |= DT_BUFFER_TOO_SMALL;
				}
				neighbourNode->flags = DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}
		}
	}

	*resultCount = n;

	return status;
}

/**
 * @brief Finds polygons around a shape on the navigation mesh.
 *
 * This method is used to find polygons around a shape defined by a set of vertices on the navigation mesh. The order of the result set is from the least to the highest cost to reach the polygons.
 *
 * At least one result array must be provided. If the result arrays are too small to hold the entire result set, they will be filled to capacity.
 *
 * A common use case for this method is to perform Dijkstra searches. Candidate polygons are found by searching the graph beginning at the start polygon.
 *
 * The same intersection test restrictions that apply to the findPolysAroundCircle() method apply to this method.
 *
 * The 3D centroid of the search polygon is used as the start position for cost calculations.
 *
 * Intersection tests occur in 2D. All polygons are projected onto the xz-plane. So the y-values of the vertices do not affect intersection tests.
 *
 * @param startRef The reference of the starting polygon.
 * @param verts The vertices defining the shape on the navigation mesh.
 * @param nverts The number of vertices in the shape.
 * @param filter The query filter that specifies which polygons can be traversed.
 * @param resultRef An array to store the polygon references of the found polygons.
 * @param resultParent An array to store the parent polygon references corresponding to the found polygons.
 * @param resultCost An array to store the costs to reach the found polygons.
 * @param resultCount A pointer to the variable that will hold the number of found polygons.
 * @param maxResult The maximum number of polygons that can be stored in the result arrays.
 *
 * @return The status of the operation. Returns DT_SUCCESS if polygons are found around the shape, DT_FAILURE if there is an invalid parameter, DT_OUT_OF_NODES if the operation runs out of nodes, DT_BUFFER_TOO_SMALL if the result arrays are too small to hold the entire result set, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::findPolysAroundShape(dtPolyRef startRef, const float* verts, const int nverts,
	const dtQueryFilter* filter,
	dtPolyRef* resultRef, dtPolyRef* resultParent, float* resultCost,
	int* resultCount, const int maxResult) const
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	*resultCount = 0;

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	m_nodePool->clear();
	m_openList->clear();

	float centerPos[3] = { 0,0,0 };
	for (int i = 0; i < nverts; ++i)
		dtVadd(centerPos, centerPos, &verts[i * 3]);
	dtVscale(centerPos, centerPos, 1.0f / nverts);

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, centerPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	dtStatus status = DT_SUCCESS;

	int n = 0;
	if (n < maxResult)
	{
		if (resultRef)
			resultRef[n] = startNode->id;
		if (resultParent)
			resultParent[n] = 0;
		if (resultCost)
			resultCost[n] = 0;
		++n;
	}
	else
	{
		status |= DT_BUFFER_TOO_SMALL;
	}

	while (!m_openList->empty())
	{
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(bestRef, &bestTile, &bestPoly);

		// Get parent poly and tile.
		dtPolyRef parentRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		if (bestNode->pidx)
			parentRef = m_nodePool->getNodeAtIdx(bestNode->pidx)->id;
		if (parentRef)
			m_nav->getTileAndPolyByRefUnsafe(parentRef, &parentTile, &parentPoly);

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			const dtLink* link = &bestTile->links[i];
			dtPolyRef neighbourRef = link->ref;
			// Skip invalid neighbours and do not follow back to parent.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Expand to neighbour
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			// Do not advance if the polygon is excluded by the filter.
			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// Find edge and calc distance to the edge.
			float va[3], vb[3];
			if (!getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb))
				continue;

			// If the poly is not touching the edge to the next polygon, skip the connection it.
			float tmin, tmax;
			int segMin, segMax;
			if (!dtIntersectSegmentPoly2D(va, vb, verts, nverts, tmin, tmax, segMin, segMax))
				continue;
			if (tmin > 1.0f || tmax < 0.0f)
				continue;

			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef);
			if (!neighbourNode)
			{
				status |= DT_OUT_OF_NODES;
				continue;
			}

			if (neighbourNode->flags & DT_NODE_CLOSED)
				continue;

			// Cost
			if (neighbourNode->flags == 0)
				dtVlerp(neighbourNode->pos, va, vb, 0.5f);

			const float total = bestNode->total + dtVdist(bestNode->pos, neighbourNode->pos);

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;

			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~DT_NODE_CLOSED);
			neighbourNode->pidx = m_nodePool->getNodeIdx(bestNode);
			neighbourNode->total = total;

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				m_openList->modify(neighbourNode);
			}
			else
			{
				if (n < maxResult)
				{
					if (resultRef)
						resultRef[n] = neighbourNode->id;
					if (resultParent)
						resultParent[n] = m_nodePool->getNodeAtIdx(neighbourNode->pidx)->id;
					if (resultCost)
						resultCost[n] = neighbourNode->total;
					++n;
				}
				else
				{
					status |= DT_BUFFER_TOO_SMALL;
				}
				neighbourNode->flags = DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}
		}
	}

	*resultCount = n;

	return status;
}

/**
 * @brief Finds polygons in the local neighborhood of a point on the navigation mesh.
 *
 * This method is optimized for a small search radius and a small number of result polygons. Candidate polygons are found by searching the navigation graph beginning at the start polygon.
 *
 * The same intersection test restrictions that apply to the `findPolysAroundCircle` method apply to this method.
 *
 * The value of the center point is used as the start point for cost calculations. It is not projected onto the surface of the mesh, so its y-value will affect the costs.
 *
 * Intersection tests occur in 2D. All polygons and the search circle are projected onto the xz-plane. So the y-value of the center point does not affect intersection tests.
 *
 * If the result arrays are too small to hold the entire result set, they will be filled to capacity.
 *
 * @param startRef The reference of the starting polygon.
 * @param centerPos The center position around which to search for polygons.
 * @param radius The search radius for finding nearby polygons.
 * @param filter The query filter that specifies which polygons can be traversed.
 * @param resultRef An array to store the polygon references of the found polygons.
 * @param resultParent An array to store the parent polygon references corresponding to the found polygons.
 * @param resultCount A pointer to the variable that will hold the number of found polygons.
 * @param maxResult The maximum number of polygons that can be stored in the result arrays.
 *
 * @return The status of the operation. Returns DT_SUCCESS if polygons are found in the local neighborhood, DT_FAILURE if there is an invalid parameter, DT_OUT_OF_NODES if the operation runs out of nodes, DT_BUFFER_TOO_SMALL if the result arrays are too small to hold the entire result set, or an error code on other failures.
 */
dtStatus dtNavMeshQuery::findLocalNeighbourhood(dtPolyRef startRef, const float* centerPos, const float radius,
	const dtQueryFilter* filter,
	dtPolyRef* resultRef, dtPolyRef* resultParent,
	int* resultCount, const int maxResult) const
{
	dtAssert(m_nav);
	dtAssert(m_tinyNodePool);

	*resultCount = 0;

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	static const int MAX_STACK = 48;
	dtNode* stack[MAX_STACK];
	int nstack = 0;

	m_tinyNodePool->clear();

	dtNode* startNode = m_tinyNodePool->getNode(startRef);
	startNode->pidx = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_CLOSED;
	stack[nstack++] = startNode;

	const float radiusSqr = dtSqr(radius);

	float pa[DT_VERTS_PER_POLYGON * 3];
	float pb[DT_VERTS_PER_POLYGON * 3];

	dtStatus status = DT_SUCCESS;

	int n = 0;
	if (n < maxResult)
	{
		resultRef[n] = startNode->id;
		if (resultParent)
			resultParent[n] = 0;
		++n;
	}
	else
	{
		status |= DT_BUFFER_TOO_SMALL;
	}

	while (nstack)
	{
		// Pop front.
		dtNode* curNode = stack[0];
		for (int i = 0; i < nstack - 1; ++i)
			stack[i] = stack[i + 1];
		nstack--;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef curRef = curNode->id;
		const dtMeshTile* curTile = 0;
		const dtPoly* curPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(curRef, &curTile, &curPoly);

		for (unsigned int i = curPoly->firstLink; i != DT_NULL_LINK; i = curTile->links[i].next)
		{
			const dtLink* link = &curTile->links[i];
			dtPolyRef neighbourRef = link->ref;
			// Skip invalid neighbours.
			if (!neighbourRef)
				continue;

			// Skip if cannot alloca more nodes.
			dtNode* neighbourNode = m_tinyNodePool->getNode(neighbourRef);
			if (!neighbourNode)
				continue;
			// Skip visited.
			if (neighbourNode->flags & DT_NODE_CLOSED)
				continue;

			// Expand to neighbour
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			// Skip off-mesh connections.
			if (neighbourPoly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
				continue;

			// Do not advance if the polygon is excluded by the filter.
			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			// Find edge and calc distance to the edge.
			float va[3], vb[3];
			if (!getPortalPoints(curRef, curPoly, curTile, neighbourRef, neighbourPoly, neighbourTile, va, vb))
				continue;

			// If the circle is not touching the next polygon, skip it.
			float tseg;
			float distSqr = dtDistancePtSegSqr2D(centerPos, va, vb, tseg);
			if (distSqr > radiusSqr)
				continue;

			// Mark node visited, this is done before the overlap test so that
			// we will not visit the poly again if the test fails.
			neighbourNode->flags |= DT_NODE_CLOSED;
			neighbourNode->pidx = m_tinyNodePool->getNodeIdx(curNode);

			// Check that the polygon does not collide with existing polygons.

			// Collect vertices of the neighbour poly.
			const int npa = neighbourPoly->vertCount;
			for (int k = 0; k < npa; ++k)
				dtVcopy(&pa[k * 3], &neighbourTile->verts[neighbourPoly->verts[k] * 3]);

			bool overlap = false;
			for (int j = 0; j < n; ++j)
			{
				dtPolyRef pastRef = resultRef[j];

				// Connected polys do not overlap.
				bool connected = false;
				for (unsigned int k = curPoly->firstLink; k != DT_NULL_LINK; k = curTile->links[k].next)
				{
					if (curTile->links[k].ref == pastRef)
					{
						connected = true;
						break;
					}
				}
				if (connected)
					continue;

				// Potentially overlapping.
				const dtMeshTile* pastTile = 0;
				const dtPoly* pastPoly = 0;
				m_nav->getTileAndPolyByRefUnsafe(pastRef, &pastTile, &pastPoly);

				// Get vertices and test overlap
				const int npb = pastPoly->vertCount;
				for (int k = 0; k < npb; ++k)
					dtVcopy(&pb[k * 3], &pastTile->verts[pastPoly->verts[k] * 3]);

				if (dtOverlapPolyPoly2D(pa, npa, pb, npb))
				{
					overlap = true;
					break;
				}
			}
			if (overlap)
				continue;

			// This poly is fine, store and advance to the poly.
			if (n < maxResult)
			{
				resultRef[n] = neighbourRef;
				if (resultParent)
					resultParent[n] = curRef;
				++n;
			}
			else
			{
				status |= DT_BUFFER_TOO_SMALL;
			}

			if (nstack < MAX_STACK)
			{
				stack[nstack++] = neighbourNode;
			}
		}
	}

	*resultCount = n;

	return status;
}


/**
 * @brief Represents an interval along a segment of a polygon boundary.
 *
 * The dtSegInterval structure is used to store information about intervals along a segment of a polygon boundary. It holds the reference of the polygon, the start time (tmin), and the end time (tmax) of the interval.
 */
struct dtSegInterval
{
	dtPolyRef ref;
	short tmin, tmax;
};

/**
 * @brief Inserts a new interval into an array of intervals while maintaining order.
 *
 * This function inserts a new interval specified by its start time (tmin), end time (tmax), and polygon reference (ref) into an array of intervals (ints) while ensuring that the intervals remain sorted by their start times. The array is limited to a maximum number of intervals (maxInts).
 *
 * @param ints A pointer to the array of intervals.
 * @param nints A reference to the number of intervals in the array (updated after insertion).
 * @param maxInts The maximum number of intervals the array can hold.
 * @param tmin The start time of the new interval.
 * @param tmax The end time of the new interval.
 * @param ref The reference of the polygon associated with the interval.
 */
static void insertInterval(dtSegInterval* ints, int& nints, const int maxInts,
	const short tmin, const short tmax, const dtPolyRef ref)
{
	if (nints + 1 > maxInts) return;
	// Find insertion point.
	int idx = 0;
	while (idx < nints)
	{
		if (tmax <= ints[idx].tmin)
			break;
		idx++;
	}
	// Move current results.
	if (nints - idx)
		memmove(ints + idx + 1, ints + idx, sizeof(dtSegInterval) * (nints - idx));
	// Store
	ints[idx].ref = ref;
	ints[idx].tmin = tmin;
	ints[idx].tmax = tmax;
	nints++;
}

/**
 * @brief Retrieves wall segments for a given polygon and filter.
 *
 * This method calculates and retrieves wall segments for a specified polygon based on the provided filter.
 * Wall segments represent the edges of the polygon that are adjacent to impassable areas.
 *
 * If the `segmentRefs` parameter is provided, then all polygon segments will be returned. Otherwise, only the wall segments are returned.
 *
 * A segment that is normally a portal will be included in the result set as a wall if the `filter` results in the neighbor polygon becoming impassable.
 *
 * The `segmentVerts` and `segmentRefs` buffers should normally be sized for the maximum segments per polygon of the source navigation mesh.
 *
 * @param ref The reference of the polygon for which wall segments are retrieved.
 * @param filter The query filter to determine which edges are considered impassable.
 * @param segmentVerts A pointer to the buffer that will store the segment vertices (allocated by the caller).
 * @param segmentRefs A pointer to the buffer that will store the segment polygon references (allocated by the caller).
 * @param segmentCount A pointer to the variable that will store the number of segments found.
 * @param maxSegments The maximum number of segments the buffers can hold.
 *
 * @return The status of the operation, indicating success or failure, and whether the buffer size is too small.
 */
dtStatus dtNavMeshQuery::getPolyWallSegments(dtPolyRef ref, const dtQueryFilter* filter,
	float* segmentVerts, dtPolyRef* segmentRefs, int* segmentCount,
	const int maxSegments) const
{
	dtAssert(m_nav);

	*segmentCount = 0;

	const dtMeshTile* tile = 0;
	const dtPoly* poly = 0;
	if (dtStatusFailed(m_nav->getTileAndPolyByRef(ref, &tile, &poly)))
		return DT_FAILURE | DT_INVALID_PARAM;

	int n = 0;
	static const int MAX_INTERVAL = 16;
	dtSegInterval ints[MAX_INTERVAL];
	int nints;

	const bool storePortals = segmentRefs != 0;

	dtStatus status = DT_SUCCESS;

	for (int i = 0, j = (int)poly->vertCount - 1; i < (int)poly->vertCount; j = i++)
	{
		// Skip non-solid edges.
		nints = 0;
		if (poly->neis[j] & DT_EXT_LINK)
		{
			// Tile border.
			for (unsigned int k = poly->firstLink; k != DT_NULL_LINK; k = tile->links[k].next)
			{
				const dtLink* link = &tile->links[k];
				if (link->edge == j)
				{
					if (link->ref != 0)
					{
						const dtMeshTile* neiTile = 0;
						const dtPoly* neiPoly = 0;
						m_nav->getTileAndPolyByRefUnsafe(link->ref, &neiTile, &neiPoly);
						if (filter->passFilter(link->ref, neiTile, neiPoly))
						{
							insertInterval(ints, nints, MAX_INTERVAL, link->bmin, link->bmax, link->ref);
						}
					}
				}
			}
		}
		else
		{
			// Internal edge
			dtPolyRef neiRef = 0;
			if (poly->neis[j])
			{
				const unsigned int idx = (unsigned int)(poly->neis[j] - 1);
				neiRef = m_nav->getPolyRefBase(tile) | idx;
				if (!filter->passFilter(neiRef, tile, &tile->polys[idx]))
					neiRef = 0;
			}

			// If the edge leads to another polygon and portals are not stored, skip.
			if (neiRef != 0 && !storePortals)
				continue;

			if (n < maxSegments)
			{
				const float* vj = &tile->verts[poly->verts[j] * 3];
				const float* vi = &tile->verts[poly->verts[i] * 3];
				float* seg = &segmentVerts[n * 6];
				dtVcopy(seg + 0, vj);
				dtVcopy(seg + 3, vi);
				if (segmentRefs)
					segmentRefs[n] = neiRef;
				n++;
			}
			else
			{
				status |= DT_BUFFER_TOO_SMALL;
			}

			continue;
		}

		// Add sentinels
		insertInterval(ints, nints, MAX_INTERVAL, -1, 0, 0);
		insertInterval(ints, nints, MAX_INTERVAL, 255, 256, 0);

		// Store segments.
		const float* vj = &tile->verts[poly->verts[j] * 3];
		const float* vi = &tile->verts[poly->verts[i] * 3];
		for (int k = 1; k < nints; ++k)
		{
			// Portal segment.
			if (storePortals && ints[k].ref)
			{
				const float tmin = ints[k].tmin / 255.0f;
				const float tmax = ints[k].tmax / 255.0f;
				if (n < maxSegments)
				{
					float* seg = &segmentVerts[n * 6];
					dtVlerp(seg + 0, vj, vi, tmin);
					dtVlerp(seg + 3, vj, vi, tmax);
					if (segmentRefs)
						segmentRefs[n] = ints[k].ref;
					n++;
				}
				else
				{
					status |= DT_BUFFER_TOO_SMALL;
				}
			}

			// Wall segment.
			const int imin = ints[k - 1].tmax;
			const int imax = ints[k].tmin;
			if (imin != imax)
			{
				const float tmin = imin / 255.0f;
				const float tmax = imax / 255.0f;
				if (n < maxSegments)
				{
					float* seg = &segmentVerts[n * 6];
					dtVlerp(seg + 0, vj, vi, tmin);
					dtVlerp(seg + 3, vj, vi, tmax);
					if (segmentRefs)
						segmentRefs[n] = 0;
					n++;
				}
				else
				{
					status |= DT_BUFFER_TOO_SMALL;
				}
			}
		}
	}

	*segmentCount = n;

	return status;
}

/**
 * @brief Finds the distance to the nearest wall from a given point within a specified radius.
 *
 * This method calculates the distance to the nearest wall from a specified point within a given radius.
 *
 * @note The @p hitPos is not adjusted using the height detail data.
 *
 * @note The @p hitDist will equal the search radius if there is no wall within the radius.
 * In this case, the values of @p hitPos and @p hitNormal are undefined.
 *
 * @note The normal will become unpredictable if @p hitDist is a very small number.
 *
 * @param startRef The reference of the starting polygon.
 * @param centerPos The center position from which to search for the nearest wall.
 * @param maxRadius The maximum search radius.
 * @param filter The query filter to determine which edges are considered impassable.
 * @param hitDist A pointer to the variable that will store the distance to the nearest wall.
 * @param hitPos A pointer to the buffer that will store the position of the hit point on the wall (allocated by the caller).
 * @param hitNormal A pointer to the buffer that will store the normal vector of the wall at the hit point (allocated by the caller).
 *
 * @return The status of the operation, indicating success or failure.
 */
dtStatus dtNavMeshQuery::findDistanceToWall(dtPolyRef startRef, const float* centerPos, const float maxRadius,
	const dtQueryFilter* filter,
	float* hitDist, float* hitPos, float* hitNormal) const
{
	dtAssert(m_nav);
	dtAssert(m_nodePool);
	dtAssert(m_openList);

	// Validate input
	if (!startRef || !m_nav->isValidPolyRef(startRef))
		return DT_FAILURE | DT_INVALID_PARAM;

	m_nodePool->clear();
	m_openList->clear();

	dtNode* startNode = m_nodePool->getNode(startRef);
	dtVcopy(startNode->pos, centerPos);
	startNode->pidx = 0;
	startNode->cost = 0;
	startNode->total = 0;
	startNode->id = startRef;
	startNode->flags = DT_NODE_OPEN;
	m_openList->push(startNode);

	float radiusSqr = dtSqr(maxRadius);

	dtStatus status = DT_SUCCESS;

	while (!m_openList->empty())
	{
		dtNode* bestNode = m_openList->pop();
		bestNode->flags &= ~DT_NODE_OPEN;
		bestNode->flags |= DT_NODE_CLOSED;

		// Get poly and tile.
		// The API input has been cheked already, skip checking internal data.
		const dtPolyRef bestRef = bestNode->id;
		const dtMeshTile* bestTile = 0;
		const dtPoly* bestPoly = 0;
		m_nav->getTileAndPolyByRefUnsafe(bestRef, &bestTile, &bestPoly);

		// Get parent poly and tile.
		dtPolyRef parentRef = 0;
		const dtMeshTile* parentTile = 0;
		const dtPoly* parentPoly = 0;
		if (bestNode->pidx)
			parentRef = m_nodePool->getNodeAtIdx(bestNode->pidx)->id;
		if (parentRef)
			m_nav->getTileAndPolyByRefUnsafe(parentRef, &parentTile, &parentPoly);

		// Hit test walls.
		for (int i = 0, j = (int)bestPoly->vertCount - 1; i < (int)bestPoly->vertCount; j = i++)
		{
			// Skip non-solid edges.
			if (bestPoly->neis[j] & DT_EXT_LINK)
			{
				// Tile border.
				bool solid = true;
				for (unsigned int k = bestPoly->firstLink; k != DT_NULL_LINK; k = bestTile->links[k].next)
				{
					const dtLink* link = &bestTile->links[k];
					if (link->edge == j)
					{
						if (link->ref != 0)
						{
							const dtMeshTile* neiTile = 0;
							const dtPoly* neiPoly = 0;
							m_nav->getTileAndPolyByRefUnsafe(link->ref, &neiTile, &neiPoly);
							if (filter->passFilter(link->ref, neiTile, neiPoly))
								solid = false;
						}
						break;
					}
				}
				if (!solid) continue;
			}
			else if (bestPoly->neis[j])
			{
				// Internal edge
				const unsigned int idx = (unsigned int)(bestPoly->neis[j] - 1);
				const dtPolyRef ref = m_nav->getPolyRefBase(bestTile) | idx;
				if (filter->passFilter(ref, bestTile, &bestTile->polys[idx]))
					continue;
			}

			// Calc distance to the edge.
			const float* vj = &bestTile->verts[bestPoly->verts[j] * 3];
			const float* vi = &bestTile->verts[bestPoly->verts[i] * 3];
			float tseg;
			float distSqr = dtDistancePtSegSqr2D(centerPos, vj, vi, tseg);

			// Edge is too far, skip.
			if (distSqr > radiusSqr)
				continue;

			// Hit wall, update radius.
			radiusSqr = distSqr;
			// Calculate hit pos.
			hitPos[0] = vj[0] + (vi[0] - vj[0]) * tseg;
			hitPos[1] = vj[1] + (vi[1] - vj[1]) * tseg;
			hitPos[2] = vj[2] + (vi[2] - vj[2]) * tseg;
		}

		for (unsigned int i = bestPoly->firstLink; i != DT_NULL_LINK; i = bestTile->links[i].next)
		{
			const dtLink* link = &bestTile->links[i];
			dtPolyRef neighbourRef = link->ref;
			// Skip invalid neighbours and do not follow back to parent.
			if (!neighbourRef || neighbourRef == parentRef)
				continue;

			// Expand to neighbour.
			const dtMeshTile* neighbourTile = 0;
			const dtPoly* neighbourPoly = 0;
			m_nav->getTileAndPolyByRefUnsafe(neighbourRef, &neighbourTile, &neighbourPoly);

			// Skip off-mesh connections.
			if (neighbourPoly->getType() == DT_POLYTYPE_OFFMESH_CONNECTION)
				continue;

			// Calc distance to the edge.
			const float* va = &bestTile->verts[bestPoly->verts[link->edge] * 3];
			const float* vb = &bestTile->verts[bestPoly->verts[(link->edge + 1) % bestPoly->vertCount] * 3];
			float tseg;
			float distSqr = dtDistancePtSegSqr2D(centerPos, va, vb, tseg);

			// If the circle is not touching the next polygon, skip it.
			if (distSqr > radiusSqr)
				continue;

			if (!filter->passFilter(neighbourRef, neighbourTile, neighbourPoly))
				continue;

			dtNode* neighbourNode = m_nodePool->getNode(neighbourRef);
			if (!neighbourNode)
			{
				status |= DT_OUT_OF_NODES;
				continue;
			}

			if (neighbourNode->flags & DT_NODE_CLOSED)
				continue;

			// Cost
			if (neighbourNode->flags == 0)
			{
				getEdgeMidPoint(bestRef, bestPoly, bestTile,
					neighbourRef, neighbourPoly, neighbourTile, neighbourNode->pos);
			}

			const float total = bestNode->total + dtVdist(bestNode->pos, neighbourNode->pos);

			// The node is already in open list and the new result is worse, skip.
			if ((neighbourNode->flags & DT_NODE_OPEN) && total >= neighbourNode->total)
				continue;

			neighbourNode->id = neighbourRef;
			neighbourNode->flags = (neighbourNode->flags & ~DT_NODE_CLOSED);
			neighbourNode->pidx = m_nodePool->getNodeIdx(bestNode);
			neighbourNode->total = total;

			if (neighbourNode->flags & DT_NODE_OPEN)
			{
				m_openList->modify(neighbourNode);
			}
			else
			{
				neighbourNode->flags |= DT_NODE_OPEN;
				m_openList->push(neighbourNode);
			}
		}
	}

	// Calc hit normal.
	dtVsub(hitNormal, centerPos, hitPos);
	dtVnormalize(hitNormal);

	*hitDist = dtMathSqrtf(radiusSqr);

	return status;
}

/**
 * @brief Checks if a polygon reference is valid and passes the query filter.
 *
 * This method checks if a given polygon reference is valid and passes the provided query filter.
 * It is used to validate polygon references before using them in queries.
 *
 * @param ref The polygon reference to check.
 * @param filter The query filter to determine if the polygon is passable.
 *
 * @return True if the polygon reference is valid and passes the query filter, false otherwise.
 */
bool dtNavMeshQuery::isValidPolyRef(dtPolyRef ref, const dtQueryFilter* filter) const
{
	const dtMeshTile* tile = 0;
	const dtPoly* poly = 0;
	dtStatus status = m_nav->getTileAndPolyByRef(ref, &tile, &poly);
	// If cannot get polygon, assume it does not exists and boundary is invalid.
	if (dtStatusFailed(status))
		return false;
	// If cannot pass filter, assume flags has changed and boundary is invalid.
	if (!filter->passFilter(ref, tile, poly))
		return false;
	return true;
}

/**
 * @brief Checks if a polygon reference is in the closed list of a navigation graph search.
 *
 * The closed list is the list of polygons that were fully evaluated during the last navigation graph search,
 * such as A* or Dijkstra.
 *
 * @param ref The polygon reference to check.
 *
 * @return True if the polygon reference is in the closed list, false otherwise.
 */
bool dtNavMeshQuery::isInClosedList(dtPolyRef ref) const
{
	if (!m_nodePool) return false;

	dtNode* nodes[DT_MAX_STATES_PER_NODE];
	int n = m_nodePool->findNodes(ref, nodes, DT_MAX_STATES_PER_NODE);

	for (int i = 0; i < n; i++)
	{
		if (nodes[i]->flags & DT_NODE_CLOSED)
			return true;
	}

	return false;
}
