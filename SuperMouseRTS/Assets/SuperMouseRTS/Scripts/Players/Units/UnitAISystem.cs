using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class UnitAISystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem entityCommandBuffer;
    public EntityArchetype oreEvent;
    public EntityArchetype loadResources;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        oreEvent = EntityManager.CreateArchetype(typeof(UnitEvent), typeof(TilePosition), typeof(OreResources));
    }

    [BurstCompile]
    struct UnitAISystemJob : IJobForEachWithEntity<Translation, MovementSpeed, UnitTarget, OreCapacity, OwnerBuilding, OperationCapability>
    {
        public EntityArchetype oreEventArchetype;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public float deltaTime;
        public float speed;

        public void Execute(Entity ent, int index, [ReadOnly]ref Translation trans, ref MovementSpeed movement, [ReadOnly] ref UnitTarget target, ref OreCapacity capacity, [ReadOnly] ref OwnerBuilding owner, ref OperationCapability operationCapability)
        {
            int2 targetPosition = target.Value.Value;
            bool usedOperation = false;
            bool canAct = operationCapability.Cooldown <= 0;

            switch (target.Operation)
            {
                case AIOperation.Unassigned:
                    break;
                case AIOperation.Attack:
                    break;
                case AIOperation.Collect:
                    if (capacity.Value >= capacity.Maximum)
                    {
                        targetPosition = owner.owner.Value;
                        if(canAct && HandleOreVicinity(ent, index, trans, capacity.Value, operationCapability, targetPosition))
                        {
                            capacity.Value = 0;
                            usedOperation = true;
                        }
                    }
                    else
                    {
                        if(canAct && HandleOreVicinity(ent, index, trans, -GameManager.HAULING_SPEED, operationCapability, targetPosition))
                        {
                            capacity.Value = (int)math.min(capacity.Maximum, capacity.Value + GameManager.HAULING_SPEED);

                            usedOperation = true;
                        }

                    }
                    break;
                case AIOperation.Repair:
                    break;
                default:
                    throw new System.Exception("Nopety nope");
            }

            if (usedOperation)
            {
                operationCapability.Cooldown = GameManager.COOLDOWN_LENGTH; 
            }

            float2 vec = new float2(targetPosition.x * GameManager.TILE_SIZE - trans.Value.x, targetPosition.y * GameManager.TILE_SIZE - trans.Value.z);
            movement.Value = math.normalizesafe(vec) * speed;

        }

        private bool HandleOreVicinity(Entity ent, int index, Translation trans, int oreChange, OperationCapability range, int2 targetPosition)
        {
            float2 diff = new float2(trans.Value.x / GameManager.TILE_SIZE, trans.Value.z / GameManager.TILE_SIZE);
            float distance = math.distance(diff, targetPosition);

            if (distance <= range.Value)
            {
                Entity newOreEvent = entityCommandBuffer.CreateEntity(index, oreEventArchetype);
                entityCommandBuffer.SetComponent(index, newOreEvent, new OreResources(oreChange));
                return true;
            }
            return false;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new UnitAISystemJob()
        {
            oreEventArchetype = this.oreEvent,
            deltaTime = Time.DeltaTime,
            speed = GameManager.MOVEMENT_SPEED,
            entityCommandBuffer = entityCommandBuffer.CreateCommandBuffer().ToConcurrent()
        };

        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);
    }
}