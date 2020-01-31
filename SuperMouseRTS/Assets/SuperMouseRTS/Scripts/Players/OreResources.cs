using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OreResources : IComponentData
{
    public int Value;

    public OreResources(int value)
    {
        Value = value;
    }
}
