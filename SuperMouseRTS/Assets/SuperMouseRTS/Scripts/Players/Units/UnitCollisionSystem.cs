using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(MovementSystem))]
[UpdateAfter(typeof(GetNearestUnitSystem))]
public class UnitCollisionSystem : JobComponentSystem
{
    [BurstCompile]
    struct UnitCollisionSystemJob : IJobForEachWithEntity<Translation, MovementSpeed, NearestUnit>
    {
        public float UnitRadius;
        public float DeltaTime;

        public void Execute(Entity ent, int index, ref Translation translation, ref MovementSpeed speed, [ReadOnly] ref NearestUnit nearest)
        {
            var speedVector = WorldCoordinateTools.UnityCoordinateAsWorld(speed.Value.x, speed.Value.y) * DeltaTime;
            var newPosition = translation.Value + speedVector;

            if (!(nearest.Ally.Direction.x == 0 && nearest.Ally.Direction.y == 0 && nearest.Ally.Direction.z == 0))
            {
                var nearestAllyPosition = translation.Value + nearest.Ally.Direction;
                float newDist = math.distance(newPosition, nearestAllyPosition);
                if (newDist < UnitRadius * 2.0f && newDist > 0.0f)
                {
                    var dirToAlly = math.normalizesafe(nearestAllyPosition - translation.Value);
                    var sideStepDir = float2(dirToAlly.z, -dirToAlly.x);

                    //if (ent.Index % 2 == 0)
                    //{
                    //    sideStepDir = -sideStepDir;
                    //}

                    // This causes lots of stuttering in large clusters
                    float3 dir = math.normalizesafe(-nearest.Ally.Direction);
                    translation.Value = nearestAllyPosition + dir * UnitRadius * 2.0f;

                    var rnd = new Random((uint)ent.Index);
                    speed.Value = speed.Value * 0.2f + sideStepDir * math.length(speed.Value) * rnd.NextFloat(0.3f, 1.0f);
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new UnitCollisionSystemJob()
        {
            UnitRadius = 0.1f,
            DeltaTime = Time.DeltaTime,
        };

        return job.Schedule(this, inputDependencies);
    }
}