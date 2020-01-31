using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public class WorldConversionTools
{
    public static int2 UnityCoordinateToWorld(float3 position, float tileSize = 1.5f)
    {
        return new int2(Mathf.FloorToInt((position.x + Mathf.Epsilon) / tileSize), Mathf.FloorToInt((position.z + Mathf.Epsilon) / tileSize));
    }


    public static float3 WorldToUnityCoordinate(int2 position, float tileSize = 1.5f)
    {
        return new float3(position.x * tileSize, 0, position.y * tileSize);
    }
}
