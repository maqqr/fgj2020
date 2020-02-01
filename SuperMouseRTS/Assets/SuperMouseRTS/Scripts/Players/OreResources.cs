using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OreResources : IComponentData, IValue
{
    public int Value;

    public OreResources(int value)
    {
        Value = value;
    }

    public int ValueProperty { get => Value; set => Value = value; }
}
