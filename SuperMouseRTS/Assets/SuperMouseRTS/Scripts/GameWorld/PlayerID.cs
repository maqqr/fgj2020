using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerID : IComponentData
{
    public int Value;

    public PlayerID(int playerID)
    {
        Value = playerID;
    }
}
