using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct SpawnScheduler : IComponentData
{
    public int SpawnsOrdered;
    public float TimeLeftToSpawn;


    public SpawnScheduler(int spanwsOrdered, float timeLeftToSpawn)
    {
        this.SpawnsOrdered = spanwsOrdered;
        TimeLeftToSpawn = timeLeftToSpawn;
    }
}
