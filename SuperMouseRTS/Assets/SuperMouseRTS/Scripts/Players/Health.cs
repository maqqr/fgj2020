using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Health : IComponentData
{
    public int Value;
    public int Maximum;

    public Health(int value, int maximum)
    {
        Value = value;
        Maximum = maximum;
    }
}
