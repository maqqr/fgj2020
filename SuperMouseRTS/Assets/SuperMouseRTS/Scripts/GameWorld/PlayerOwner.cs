using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerOwner : IComponentData
{
    public int PlayerID;

    public PlayerOwner(int playerID)
    {
        PlayerID = playerID;
    }
}
