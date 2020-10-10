using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Nearest
{
    public Entity Entity;
    public float3 Direction;
    public float3 Position;
}

[Serializable]
public struct NearestUnit : IComponentData
{
    public Nearest Ally;
    public Nearest Enemy;
}
