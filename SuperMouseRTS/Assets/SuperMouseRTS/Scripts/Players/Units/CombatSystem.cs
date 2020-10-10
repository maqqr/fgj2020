using System.Numerics;
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
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public float DeltaTime;

        public void Execute(Entity ent, int index, ref MovementSpeed speed, ref OperationCapability capability, [ReadOnly] ref NearestUnit nearest)
        {
            if (!(nearest.Enemy.Direction.x == 0 && nearest.Enemy.Direction.y == 0 && nearest.Enemy.Direction.z == 0))
            {
                float dist = math.length(nearest.Enemy.Direction);
                var dir = math.normalizesafe(nearest.Enemy.Direction);
                if (dist < 2.5f * GameManager.TILE_SIZE)
                {
                    speed.Value = float2(dir.x, dir.z);
                }

                if (dist < 0.8f * GameManager.TILE_SIZE)
                {
                    speed.Value = float2(0, 0);

                    capability.Cooldown -= DeltaTime;
                    if (capability.Cooldown <= 0f)
                    {
                        capability.Cooldown = GameManager.COOLDOWN_LENGTH;
                        Entity ev = CommandBuffer.CreateEntity(index);
                        CommandBuffer.AddComponent<DamageEvent>(index, ev);
                        CommandBuffer.SetComponent(index, ev, new DamageEvent() { Target = nearest.Enemy.Entity });

                        // Create bullet
                        float3 bulletEnd = nearest.Enemy.Position;
                        float3 bulletStart = bulletEnd - nearest.Enemy.Direction;

                        float bulletLife = dist / GameManager.BULLET_VELOCITY;
                        float3 speed3 = dir * GameManager.BULLET_VELOCITY;
                        float2 speed2 = float2(speed3.x, speed3.z);

                        Entity bulletEntity = CommandBuffer.CreateEntity(index);
                        CommandBuffer.AddComponent(index, bulletEntity, new Bullet { LifeTimeLeft = bulletLife });
                        CommandBuffer.AddComponent(index, bulletEntity, new Translation { Value = bulletStart });
                        CommandBuffer.AddComponent(index, bulletEntity, new MovementSpeed { Value = speed2 });
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
            CommandBuffer = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter(),
            DeltaTime = Time.DeltaTime,
        };

        var handle = job.Schedule(this, inputDependencies);
        entityCommandBuffer.AddJobHandleForProducer(handle);
        return handle;
    }
}