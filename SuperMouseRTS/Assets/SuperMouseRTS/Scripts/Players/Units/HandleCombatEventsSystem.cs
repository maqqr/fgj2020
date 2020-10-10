using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class HandleCombatEventsSystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    [BurstCompile]
    struct HandleCombatEventsSystemJob : IJobForEachWithEntity<DamageEvent>
    {
        public uint InitialSeed;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(Entity ent, int index, ref DamageEvent ev)
        {
            var rnd = new Random(InitialSeed * 123 + ((uint)ent.Index) * 456);

            if (rnd.NextFloat(0.0f, 1.0f) < 0.05f)
            {
                CommandBuffer.DestroyEntity(index, ev.Target);
            }

            CommandBuffer.DestroyEntity(index, ent);
        }
    }

    private uint seed = 0;

    protected override void OnCreate()
    {
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new HandleCombatEventsSystemJob()
        {
            InitialSeed = seed++,
            CommandBuffer = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter()
        };


        var handle = job.Schedule(this, inputDependencies);
        entityCommandBuffer.AddJobHandleForProducer(handle);
        return handle;
    }
}