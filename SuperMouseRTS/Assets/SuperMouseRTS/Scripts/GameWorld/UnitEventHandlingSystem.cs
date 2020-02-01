using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class UnitEventHandlingSystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    //[BurstCompile]
    struct UnitEventHandlingSystemJob : IJobForEachWithEntity<UnitEvent, OreResources, TilePosition>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> tiles;
        public int tilesVertically;

        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity ent, int index, [ReadOnly] ref UnitEvent ev, [ReadOnly] ref OreResources resource, [ReadOnly] ref TilePosition pos)
        {
            int tilesIndex = WorldCoordinateTools.PositionIntoIndex(pos.Value.x, pos.Value.y, tilesVertically);
            int resOnTile = tiles[tilesIndex];
            resOnTile += resource.Value;
            tiles[tilesIndex] = resOnTile;

            entityCommandBuffer.DestroyEntity(tilesIndex, ent);
        }
    }

    struct HandleEventResultsOnTotals : IJobForEachWithEntity<TilePosition, OreResources>
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<int> tiles;
        public int tilesVertically;

        public void Execute(Entity entity, int index, ref TilePosition tile, ref OreResources resources)
        {
            resources.Value += tiles[WorldCoordinateTools.PositionIntoIndex(tile.Value.x, tile.Value.y, tilesVertically)];
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        WorldStatusSystem sys = World.GetOrCreateSystem<WorldStatusSystem>();
        if (!sys.IsTileCacheReady)
        {
            return inputDependencies;
        }
        var resourceChanges = new NativeArray<int>(sys.TileCache.Length, Allocator.TempJob);


        var unitTotalCountingSystem = new UnitEventHandlingSystemJob()
        {
            entityCommandBuffer = this.entityCommandBuffer.CreateCommandBuffer().ToConcurrent(),
            tiles = resourceChanges,
            tilesVertically = GameManager.Instance.LoadedSettings.TilesVertically
        };
        
        var eventResultHandling = new HandleEventResultsOnTotals()
        {
            tilesVertically = GameManager.Instance.LoadedSettings.TilesVertically,
            tiles = resourceChanges
        };
        inputDependencies = unitTotalCountingSystem.Schedule(this, inputDependencies);        
        inputDependencies = eventResultHandling.Schedule(this, inputDependencies);

        entityCommandBuffer.AddJobHandleForProducer(inputDependencies);

        return inputDependencies;
    }
}