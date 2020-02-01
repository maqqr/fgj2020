using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Player : IComponentData
{
    public bool LeftDownOnLastFrame;
    public bool RightDownOnLastFrame;
}
