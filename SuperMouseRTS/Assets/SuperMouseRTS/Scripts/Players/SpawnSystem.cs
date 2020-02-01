using System;
using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class SpawnSystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem entityCommandBuffer;
    private EntityArchetype archetype;

    protected override void OnCreate()
    {
        archetype = EntityManager.CreateArchetype(typeof(Translation), typeof(Rotation), typeof(PlayerID), typeof(MovementSpeed));

        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        GameManager.Instance.OnSettingsLoaded += Loaded;
        Enabled = false;
    }

    private void Loaded(Settings obj)
    {
        Enabled = true;
    }

    [BurstCompile]
    struct SpawnSystemJob : IJobForEachWithEntity<PlayerID, TilePosition, SpawnTimer>
    {
        public float deltaTime;
        public float tileSize;
        public EntityArchetype arch;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity ent, int index, [ReadOnly] ref PlayerID id, [ReadOnly] ref TilePosition tile, ref SpawnTimer timer)
        {
            if(timer.TimeLeftToSpawn < 0)
            {
                return;
            }

            timer.TimeLeftToSpawn -= deltaTime;
            if(timer.TimeLeftToSpawn <= 0)
            {
                //Really not sure if this id is the right one at all
                Entity e = entityCommandBuffer.CreateEntity(index, arch);
                entityCommandBuffer.SetComponent(index, e, new PlayerID(id.Value));
                Translation trans = new Translation();
                trans.Value = WorldConversionTools.WorldToUnityCoordinate(tile.Value, tileSize);
                entityCommandBuffer.SetComponent(index, e, trans);
                
                timer.TimeLeftToSpawn = -1;
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var job = new SpawnSystemJob();

        job.deltaTime = Time.DeltaTime; //Prkl, miten sen saa ECS:ssä ulos?
        job.entityCommandBuffer = entityCommandBuffer.CreateCommandBuffer().ToConcurrent();
        job.arch = archetype;
        job.tileSize = GameManager.Instance.LoadedSettings.TileSize;

        inputDependencies = job.Schedule(this, inputDependencies);
        entityCommandBuffer.AddJobHandleForProducer(inputDependencies);
        return inputDependencies;
    }
}