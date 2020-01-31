using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    [Serializable]
    public struct TilePosition : IComponentData
    {
        public int2 Position;

        public TilePosition(int2 position)
        {
            Position = position;
        }
    }
}