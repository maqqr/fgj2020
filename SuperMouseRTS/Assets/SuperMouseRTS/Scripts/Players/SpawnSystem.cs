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
        archetype = EntityManager.CreateArchetype(typeof(Translation), typeof(Rotation), typeof(PlayerID), typeof(MovementSpeed), typeof(OperationCapability),
                                                  typeof(Health), typeof(AttackStrength), typeof(OreCapacity), typeof(UnitTarget), typeof(OwnerBuilding));

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
    struct SpawnSystemJob : IJobForEachWithEntity<PlayerID, TilePosition, SpawnScheduler>
    {
        [ReadOnly]
        public float deltaTime;
        [ReadOnly]
        public float tileSize;
        [ReadOnly]
        public Health health;
        [ReadOnly]
        public EntityArchetype arch;
        [ReadOnly]
        public int capacity;
        [ReadOnly]
        public float rangeOfOperation;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public float UnitSpawnTime;

        public void Execute(Entity ent, int index, [ReadOnly] ref PlayerID id, [ReadOnly] ref TilePosition tile, ref SpawnScheduler timer)
        {
            timer.TimeLeftToSpawn = math.max(0, timer.TimeLeftToSpawn - deltaTime);

            if (timer.SpawnsOrdered <= 0)
            {
                return;
            }

            if (timer.TimeLeftToSpawn <= 0)
            {
                //Really not sure if this id is the right one at all
                Entity e = entityCommandBuffer.CreateEntity(index, arch);
                entityCommandBuffer.SetComponent(index, e, new PlayerID(id.Value));
                Translation trans = new Translation();
                trans.Value = WorldCoordinateTools.WorldToUnityCoordinate(tile.Value, tileSize);
                entityCommandBuffer.SetComponent(index, e, trans);
                entityCommandBuffer.SetComponent(index, e, new Health(health.Value, health.Maximum));
                entityCommandBuffer.SetComponent(index, e, new OwnerBuilding(tile));
                entityCommandBuffer.SetComponent(index, e, new OperationCapability(rangeOfOperation, 0));
                entityCommandBuffer.SetComponent(index, e, new OreCapacity(0, capacity));
                entityCommandBuffer.SetComponent(index, e, new UnitTarget(tile, Priorities.NotSet, AIOperation.Unassigned));
                entityCommandBuffer.SetComponent(index, e, new Rotation() { Value = new quaternion(0, 0f, 0f, 1f) });


                timer.SpawnsOrdered--;
                timer.TimeLeftToSpawn = UnitSpawnTime;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new SpawnSystemJob();

        job.deltaTime = Time.DeltaTime;
        job.entityCommandBuffer = entityCommandBuffer.CreateCommandBuffer().ToConcurrent();
        job.arch = archetype;
        job.capacity = GameManager.Instance.LoadedSettings.UnitCapacity;
        job.health = GameManager.Instance.LoadedSettings.UnitHealth;
        job.tileSize = GameManager.TILE_SIZE;
        job.rangeOfOperation = GameManager.Instance.LoadedSettings.UnitRangeOfOperation;
        job.UnitSpawnTime = GameManager.Instance.LoadedSettings.UnitSpawnTime;

        inputDependencies = job.Schedule(this, inputDependencies);
        entityCommandBuffer.AddJobHandleForProducer(inputDependencies);
        return inputDependencies;
    }
}