using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerSpawning : IComponentData
{
    //Flag boolean for spawning until things start working magnificently
    public bool IsSpawning;

    public PlayerSpawning(bool isSpawning)
    {
        IsSpawning = isSpawning;
    }
}
