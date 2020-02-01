//using UnityEngine;
//using System.Collections;
//using Unity.Burst;
//using Unity.Entities;
//using Assets.SuperMouseRTS.Scripts.GameWorld;

//[BurstCompile]
//struct UnitEventHandlingSystemJob<T> : IJobForEachWithEntity<UnitEvent, T, TilePosition> where T : struct, IValue 
//{
//    [NativeDisableParallelForRestriction]
//    public NativeArray<int> tiles;
//    public int tilesVertically;

//    public EntityCommandBuffer.Concurrent entityCommandBuffer;

//    public void Execute(Entity ent, int index, [ReadOnly] ref UnitEvent ev, [ReadOnly] ref OreResources resource, [ReadOnly] ref TilePosition pos)
//    {
//        int tilesIndex = WorldCoordinateTools.PositionIntoIndex(pos.Value.x, pos.Value.y, tilesVertically);
//        int resOnTile = tiles[tilesIndex];
//        resOnTile += resource.Value;
//        tiles[tilesIndex] = resOnTile;

//        entityCommandBuffer.DestroyEntity(tilesIndex, ent);
//    }
//}