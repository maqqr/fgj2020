using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    [Serializable]
    public struct TilePosition : IComponentData, IEquatable<TilePosition>
    {
        public int2 Value;

        public TilePosition(int2 position)
        {
            Value = position;
        }

        public bool Equals(TilePosition other)
        {
            return Value.x == other.Value.x && Value.y == other.Value.y;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}