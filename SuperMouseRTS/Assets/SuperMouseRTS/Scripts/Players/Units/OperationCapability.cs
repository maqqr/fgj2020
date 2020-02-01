using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OperationCapability : IComponentData
{
    public float Value;
    public float Cooldown;

    public OperationCapability(float value, float cooldown)
    {
        Value = value;
        Cooldown = cooldown;
    }
}
