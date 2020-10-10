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


    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        WorldStatusSystem sys = World.GetOrCreateSystem<WorldStatusSystem>();
        if (!sys.IsTileCacheReady)
        {
            return inputDependencies;
        }
        
        inputDependencies = HandleUnitEventForType<OreResources>(sys, inputDependencies);
        inputDependencies = HandleUnitEventForType<Health>(sys, inputDependencies);

        return inputDependencies;
    }

    private JobHandle HandleUnitEventForType<T>(WorldStatusSystem sys, JobHandle inputDependencies) where T : struct, IValue, IComponentData
    {
        var resourceChanges = new NativeArray<int>(sys.TileCache.Length, Allocator.TempJob);

        var unitTotalCountingSystem = new UnitEventHandlingSystemJob<T>()
        {
            entityCommandBuffer = this.entityCommandBuffer.CreateCommandBuffer().AsParallelWriter(),
            tiles = resourceChanges,
            tilesVertically = GameManager.Instance.LoadedSettings.TilesVertically
        };

        var eventResultHandling = new HandleEventResultsOnTotals<T>()
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