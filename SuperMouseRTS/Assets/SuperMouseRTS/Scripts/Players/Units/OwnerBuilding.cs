using Assets.SuperMouseRTS.Scripts.GameWorld;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OwnerBuilding : IComponentData
{
    public TilePosition owner;

    public OwnerBuilding(TilePosition owner)
    {
        this.owner = owner;
    }
}
