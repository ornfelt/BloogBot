#ifndef NAVIGATION_H
#define NAVIGATION_H

#include "MoveMap.h"
#include <vector>
#include <string>

/**
 * @brief Class representing a 3D point with X, Y, and Z coordinates.
 */
class XYZ
{
public:
	float X; ///< X coordinate.
	float Y; ///< Y coordinate.
	float Z; ///< Z coordinate.

	/**
	 * @brief Default constructor initializing coordinates to zero.
	 */
	XYZ()
	{
		X = 0;
		Y = 0;
		Z = 0;
	}

	/**
	 * @brief Constructor with specified coordinates.
	 *
	 * @param X The X coordinate.
	 * @param Y The Y coordinate.
	 * @param Z The Z coordinate.
	 */
	XYZ(double X, double Y, double Z)
	{
		this->X = (float)X;
		this->Y = (float)Y;
		this->Z = (float)Z;
	}
};

/**
 * @brief Class for navigation operations.
 */
class Navigation
{
public:
	/**
	 * @brief Get a singleton instance of the Navigation class.
	 *
	 * @return A pointer to the singleton instance.
	 */
	static Navigation* GetInstance();

	/**
	 * @brief Initialize the Navigation system.
	 */
	void Initialize();

	/**
	 * @brief Release resources and shutdown the Navigation system.
	 */
	void Release();

	/**
	 * @brief Calculate a path between two points on a map.
	 *
	 * @param mapId The ID of the map.
	 * @param start The starting position.
	 * @param end The destination position.
	 * @param straightPath Whether to calculate a straight path.
	 * @param length The length of the calculated path.
	 * @return An array of XYZ points representing the path.
	 */
	XYZ* CalculatePath(unsigned int mapId, XYZ start, XYZ end, bool straightPath, int* length);

	/**
	 * @brief Free the memory allocated for a path array.
	 *
	 * @param path The path array to free.
	 */
	void FreePathArr(XYZ* path);

	/**
	 * @brief Get the path to the MMAPs folder.
	 *
	 * @return The path to the MMAPs folder as a string.
	 */
	std::string GetMmapsPath();

private:
	/**
	 * @brief Initialize maps for a continent.
	 *
	 * @param manager The MMapManager instance.
	 * @param mapId The ID of the map.
	 */
	void InitializeMapsForContinent(MMAP::MMapManager* manager, unsigned int mapId);

	static Navigation* s_singletonInstance; ///< Singleton instance of the Navigation class.
	XYZ* currentPath; ///< Current calculated path.
};

#endif
