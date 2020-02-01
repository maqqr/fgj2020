using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OreCapacity : IComponentData
{
    public int Value;
    public int Maximum;

    public OreCapacity(int value, int maximum)
    {
        Value = value;
        Maximum = maximum;
    }
}
