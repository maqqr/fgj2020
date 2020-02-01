using Assets.SuperMouseRTS.Scripts.GameWorld;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum AIOperation
{
    Unassigned,
    Attack,
    Collect,
    Repair
}


public enum Priorities
{
    NotSet = -1,
    NotUrgent = 0,
    Important,
    VeryUrgent,
    PlayerOrdered
}


[Serializable]
public struct UnitTarget : IComponentData
{
    public TilePosition Value;
    public Priorities Priority;
    public AIOperation Operation;

    public UnitTarget(TilePosition value, Priorities priority, AIOperation operation)
    {
        Value = value;
        this.Priority = priority;
        this.Operation = operation;
    }
}
