using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Health : IComponentData, IValue
{
    public int Value;
    public int Maximum;

    public Health(int value, int maximum)
    {
        Value = value;
        Maximum = maximum;
    }

    public int ValueProperty { get => Value; set => Value = value; }
}
