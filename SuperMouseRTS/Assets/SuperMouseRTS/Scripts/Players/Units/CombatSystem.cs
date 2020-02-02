using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public struct DamageEvent : IComponentData
{
    public Entity Target;
}

[UpdateAfter(typeof(GetNearestUnitSystem))]
[UpdateBefore(typeof(UnitCollisionSystem))]
public class CombatSystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    [BurstCompile]
    struct CombatSystemJob : IJobForEachWithEntity<MovementSpeed, OperationCapability, NearestUnit>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float DeltaTime;

        public void Execute(Entity ent, int index, ref MovementSpeed speed, ref OperationCapability capability, [ReadOnly] ref NearestUnit nearest)
        {
            if (!(nearest.Enemy.Direction.x == 0 && nearest.Enemy.Direction.y == 0 && nearest.Enemy.Direction.z == 0))
            {
                float dist = math.length(nearest.Enemy.Direction);
                if (dist < 2.0f * GameManager.TILE_SIZE)
                {
                    var dir = math.normalizesafe(nearest.Enemy.Direction);
                    speed.Value = float2(dir.x, dir.z);
                }

                if (dist < 0.2f * GameManager.TILE_SIZE)
                {
                    capability.Cooldown -= DeltaTime;
                    if (capability.Cooldown <= 0f)
                    {
                        capability.Cooldown = GameManager.COOLDOWN_LENGTH;
                        Entity ev = CommandBuffer.CreateEntity(index);
                        CommandBuffer.AddComponent<DamageEvent>(index, ev);
                        CommandBuffer.SetComponent(index, ev, new DamageEvent() { Target = nearest.Enemy.Entity });
                    }
                }
            }
        }
    }

    protected override void OnCreate()
    {
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new CombatSystemJob()
        {
            CommandBuffer = entityCommandBuffer.CreateCommandBuffer().ToConcurrent(),
            DeltaTime = Time.DeltaTime,
        };

        var handle = job.Schedule(this, inputDependencies);
        entityCommandBuffer.AddJobHandleForProducer(handle);
        return handle;
    }
}