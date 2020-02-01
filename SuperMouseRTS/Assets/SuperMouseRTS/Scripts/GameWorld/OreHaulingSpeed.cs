using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OreHaulingSpeed : IComponentData
{
    public int Value;

    public OreHaulingSpeed(int value)
    {
        Value = value;
    }
}
