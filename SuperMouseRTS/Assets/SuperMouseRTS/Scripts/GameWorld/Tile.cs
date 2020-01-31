using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    [Serializable]
    public struct Tile : IComponentData
    {
        public TileContent tile;

        public Tile(TileContent tile)
        {
            this.tile = tile;
        }
    }
}