using UnityEngine;
using System.Collections;
using Unity.Burst;
using Unity.Entities;
using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Collections;

[BurstCompile]
struct UnitEventHandlingSystemJob<T> : IJobForEachWithEntity<UnitEvent, T, TilePosition> where T : struct, IValue, IComponentData
{
    [NativeDisableParallelForRestriction]
    public NativeArray<int> tiles;
    public int tilesVertically;

    public EntityCommandBuffer.Concurrent entityCommandBuffer;

    public void Execute(Entity ent, int index, [ReadOnly] ref UnitEvent ev, [ReadOnly] ref T resource, [ReadOnly] ref TilePosition pos)
    {
        int tilesIndex = WorldCoordinateTools.PositionIntoIndex(pos.Value.x, pos.Value.y, tilesVertically);
        int resOnTile = tiles[tilesIndex];
        resOnTile += resource.ValueProperty;
        tiles[tilesIndex] = resOnTile;

        entityCommandBuffer.DestroyEntity(tilesIndex, ent);
    }
}

struct HandleEventResultsOnTotals<T> : IJobForEachWithEntity<TilePosition, T> where T : struct, IValue, IComponentData
{
    [DeallocateOnJobCompletion]
    [ReadOnly]
    public NativeArray<int> tiles;
    public int tilesVertically;

    public void Execute(Entity entity, int index, ref TilePosition tile, ref T resources)
    {
        resources.ValueProperty += tiles[WorldCoordinateTools.PositionIntoIndex(tile.Value.x, tile.Value.y, tilesVertically)];
    }
}