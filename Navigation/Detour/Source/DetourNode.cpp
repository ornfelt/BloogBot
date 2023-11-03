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

#include "DetourNode.h"
#include "DetourAlloc.h"
#include "DetourAssert.h"
#include "DetourCommon.h"
#include <string.h>

/**
 * @brief Hashes a polygon reference value.
 *
 * This function calculates a hash value for a polygon reference.
 *
 * @param a The polygon reference to hash.
 *
 * @return The hash value.
 */
#ifdef DT_POLYREF64
 // From Thomas Wang, https://gist.github.com/badboy/6267743
inline unsigned int dtHashRef(dtPolyRef a)
{
	a = (~a) + (a << 18); // a = (a << 18) - a - 1;
	a = a ^ (a >> 31);
	a = a * 21; // a = (a + (a << 2)) + (a << 4);
	a = a ^ (a >> 11);
	a = a + (a << 6);
	a = a ^ (a >> 22);
	return (unsigned int)a;
}
#else
 /**
  * @brief Hashes a polygon reference value.
  *
  * This function calculates a hash value for a polygon reference.
  *
  * @param a The polygon reference to hash.
  *
  * @return The hash value.
  */
inline unsigned int dtHashRef(dtPolyRef a)
{
	a += ~(a << 15);
	a ^= (a >> 10);
	a += (a << 3);
	a ^= (a >> 6);
	a += ~(a << 11);
	a ^= (a >> 16);
	return (unsigned int)a;
}
#endif

//////////////////////////////////////////////////////////////////////////////////////////
/**
 * @brief Constructs a node pool for navigation mesh nodes.
 *
 * This constructor initializes a node pool with the specified maximum number of nodes and hash size.
 *
 * @param maxNodes The maximum number of nodes in the pool.
 * @param hashSize The size of the hash table for quick node lookup.
 */
dtNodePool::dtNodePool(int maxNodes, int hashSize) :
	m_nodes(0),
	m_first(0),
	m_next(0),
	m_maxNodes(maxNodes),
	m_hashSize(hashSize),
	m_nodeCount(0)
{
	dtAssert(dtNextPow2(m_hashSize) == (unsigned int)m_hashSize);
	dtAssert(m_maxNodes > 0);

	m_nodes = (dtNode*)dtAlloc(sizeof(dtNode) * m_maxNodes, DT_ALLOC_PERM);
	m_next = (dtNodeIndex*)dtAlloc(sizeof(dtNodeIndex) * m_maxNodes, DT_ALLOC_PERM);
	m_first = (dtNodeIndex*)dtAlloc(sizeof(dtNodeIndex) * hashSize, DT_ALLOC_PERM);

	dtAssert(m_nodes);
	dtAssert(m_next);
	dtAssert(m_first);

	memset(m_first, 0xff, sizeof(dtNodeIndex) * m_hashSize);
	memset(m_next, 0xff, sizeof(dtNodeIndex) * m_maxNodes);
}

/**
 * @brief Destroys the node pool and frees allocated memory.
 */
dtNodePool::~dtNodePool()
{
	dtFree(m_nodes);
	dtFree(m_next);
	dtFree(m_first);
}

/**
 * @brief Clears the node pool, resetting it to its initial state.
 */
void dtNodePool::clear()
{
	memset(m_first, 0xff, sizeof(dtNodeIndex) * m_hashSize);
	m_nodeCount = 0;
}

/**
 * @brief Finds nodes with a specific polygon reference.
 *
 * This function finds nodes with the given polygon reference and populates the provided array with pointers to those nodes.
 *
 * @param id The polygon reference to search for.
 * @param nodes An array to store pointers to found nodes.
 * @param maxNodes The maximum number of nodes to search for.
 *
 * @return The number of nodes found.
 */
unsigned int dtNodePool::findNodes(dtPolyRef id, dtNode** nodes, const int maxNodes)
{
	int n = 0;
	unsigned int bucket = dtHashRef(id) & (m_hashSize - 1);
	dtNodeIndex i = m_first[bucket];
	while (i != DT_NULL_IDX)
	{
		if (m_nodes[i].id == id)
		{
			if (n >= maxNodes)
				return n;
			nodes[n++] = &m_nodes[i];
		}
		i = m_next[i];
	}

	return n;
}

/**
 * @brief Finds a node with a specific polygon reference and state.
 *
 * This function finds a node with the given polygon reference and state.
 *
 * @param id The polygon reference to search for.
 * @param state The state to match.
 *
 * @return A pointer to the found node, or nullptr if not found.
 */
dtNode* dtNodePool::findNode(dtPolyRef id, unsigned char state)
{
	unsigned int bucket = dtHashRef(id) & (m_hashSize - 1);
	dtNodeIndex i = m_first[bucket];
	while (i != DT_NULL_IDX)
	{
		if (m_nodes[i].id == id && m_nodes[i].state == state)
			return &m_nodes[i];
		i = m_next[i];
	}
	return 0;
}

/**
 * @brief Gets or creates a node with a specific polygon reference and state.
 *
 * This function either retrieves an existing node with the given polygon reference and state or creates a new one if it doesn't exist.
 *
 * @param id The polygon reference to search for.
 * @param state The state to match.
 *
 * @return A pointer to the node, or nullptr if the maximum node count is reached.
 */
dtNode* dtNodePool::getNode(dtPolyRef id, unsigned char state)
{
	unsigned int bucket = dtHashRef(id) & (m_hashSize - 1);
	dtNodeIndex i = m_first[bucket];
	dtNode* node = 0;
	while (i != DT_NULL_IDX)
	{
		if (m_nodes[i].id == id && m_nodes[i].state == state)
			return &m_nodes[i];
		i = m_next[i];
	}

	if (m_nodeCount >= m_maxNodes)
		return 0;

	i = (dtNodeIndex)m_nodeCount;
	m_nodeCount++;

	// Init node
	node = &m_nodes[i];
	node->pidx = 0;
	node->cost = 0;
	node->total = 0;
	node->id = id;
	node->state = state;
	node->flags = 0;

	m_next[i] = m_first[bucket];
	m_first[bucket] = i;

	return node;
}

//////////////////////////////////////////////////////////////////////////////////////////
/**
 * @brief Constructs a priority queue for navigation mesh nodes.
 *
 * This constructor initializes a priority queue with the specified capacity.
 *
 * @param n The capacity of the priority queue.
 */
dtNodeQueue::dtNodeQueue(int n) :
	m_heap(0),
	m_capacity(n),
	m_size(0)
{
	dtAssert(m_capacity > 0);

	m_heap = (dtNode**)dtAlloc(sizeof(dtNode*) * (m_capacity + 1), DT_ALLOC_PERM);
	dtAssert(m_heap);
}

/**
 * @brief Destroys the priority queue and frees allocated memory.
 */
dtNodeQueue::~dtNodeQueue()
{
	dtFree(m_heap);
}

/**
 * @brief Moves a node up in the priority queue to maintain heap property.
 *
 * @param i The index of the node to move up.
 * @param node The node to move.
 */
void dtNodeQueue::bubbleUp(int i, dtNode* node)
{
	int parent = (i - 1) / 2;
	// note: (index > 0) means there is a parent
	while ((i > 0) && (m_heap[parent]->total > node->total))
	{
		m_heap[i] = m_heap[parent];
		i = parent;
		parent = (i - 1) / 2;
	}
	m_heap[i] = node;
}

/**
 * @brief Moves a node down in the priority queue to maintain heap property.
 *
 * @param i The index of the node to move down.
 * @param node The node to move.
 */
void dtNodeQueue::trickleDown(int i, dtNode* node)
{
	int child = (i * 2) + 1;
	while (child < m_size)
	{
		if (((child + 1) < m_size) &&
			(m_heap[child]->total > m_heap[child + 1]->total))
		{
			child++;
		}
		m_heap[i] = m_heap[child];
		i = child;
		child = (i * 2) + 1;
	}
	bubbleUp(i, node);
}
