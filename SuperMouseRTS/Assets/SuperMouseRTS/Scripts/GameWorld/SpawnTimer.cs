using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct SpawnTimer : IComponentData
{
    public float TimeLeftToSpawn;

    public SpawnTimer(float timeLeftToSpawn)
    {
        this.TimeLeftToSpawn = timeLeftToSpawn;
    }
}
