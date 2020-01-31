using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct TilePosition : IComponentData
{
    public int2 Position;
}
