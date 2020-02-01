using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AttackStrength : IComponentData
{
    public int Value;

    public AttackStrength(int value)
    {
        Value = value;
    }
}
