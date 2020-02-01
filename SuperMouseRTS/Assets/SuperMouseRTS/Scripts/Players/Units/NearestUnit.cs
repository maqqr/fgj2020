using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct NearestUnit : IComponentData
{
    public Entity NearestAlly;
    public float DistToAlly;

    public Entity NearestEnemy;
    public float DistToEnemy;
}
