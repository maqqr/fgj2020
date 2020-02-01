using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System;

public class WorldCoordinateTools
{
    public static int2 UnityCoordinateToWorld(float3 position, float tileSize = 1.5f)
    {
        return new int2(Mathf.FloorToInt((position.x + Mathf.Epsilon) / tileSize), Mathf.FloorToInt((position.z + Mathf.Epsilon) / tileSize));
    }


    public static float3 WorldToUnityCoordinate(int x, int y, float tileSize = 1.5f)
    {
        return new float3(x * tileSize, 0, y * tileSize);
    }


    public static float3 UnityCoordinateAsWorld(float x, float y)
    {
        return new float3(x, 0, y);
    }


    public static float3 WorldToUnityCoordinate(int2 position, float tileSize = 1.5f)
    {

        return WorldToUnityCoordinate(position.x, position.y, tileSize);
    }


    public static float LevelWidth(int tilesVertically, float tileSize = 1.5f)
    {
        return tilesVertically * tileSize;
    }

    public static float LevelHeight(int tilesHorizontally, float tileSize = 1.5f)
    {
        return tilesHorizontally * tileSize;
    }

    internal static Vector3 WorldCenter(int tilesHorizontally, int tilesVertically, float tileSize)
    {
        return new Vector3(tilesHorizontally * tileSize * 0.5f, 0, tilesVertically * tileSize * 0.5f);
    }
}
