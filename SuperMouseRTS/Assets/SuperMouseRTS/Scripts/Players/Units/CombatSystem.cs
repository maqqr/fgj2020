using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(GetNearestUnitSystem))]
[UpdateBefore(typeof(UnitCollisionSystem))]
public class CombatSystem : JobComponentSystem
{
    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct CombatSystemJob : IJobForEach<MovementSpeed, NearestUnit>
    {
        public void Execute(ref MovementSpeed speed, [ReadOnly] ref NearestUnit nearest)
        {
            if (!(nearest.Enemy.Direction.x == 0 && nearest.Enemy.Direction.y == 0 && nearest.Enemy.Direction.z == 0))
            {
                if (math.length(nearest.Enemy.Direction) < 2.0f * GameManager.TILE_SIZE)
                {
                    var dir = math.normalizesafe(nearest.Enemy.Direction);
                    speed.Value = float2(dir.x, dir.z);
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new CombatSystemJob();


        return job.Schedule(this, inputDependencies);
    }
}