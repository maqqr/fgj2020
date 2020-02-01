using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;


public class MovementSystem : JobComponentSystem
{
    [BurstCompile]
    struct MovementSystemJob : IJobForEachWithEntity<Translation, MovementSpeed, NearestUnit>
    {
        public float Tilesize;
        public float DeltaTime;
        public float UnitRadius;

        public void Execute(Entity ent, int index, ref Translation translation, [ReadOnly] ref MovementSpeed speed, [ReadOnly] ref NearestUnit nearest)
        {
            translation.Value += WorldCoordinateTools.UnityCoordinateAsWorld(speed.Value.x, speed.Value.y) * DeltaTime;;

            //var speedVector = WorldCoordinateTools.UnityCoordinateAsWorld(speed.Value.x, speed.Value.y) * DeltaTime;
            //var newPosition = translation.Value + speedVector;

            //if (!(nearest.Ally.Direction.x == 0 && nearest.Ally.Direction.y == 0 && nearest.Ally.Direction.z == 0))
            //{
            //    var nearestAllyPosition = translation.Value + nearest.Ally.Direction;
            //    float newDist = math.distance(newPosition, nearestAllyPosition);
            //    if (newDist < UnitRadius * 2.0f && newDist > 0.0f)
            //    {
            //        var sideStepSpeed = WorldCoordinateTools.UnityCoordinateAsWorld(speed.Value.y, -speed.Value.x) * DeltaTime;
            //        if (ent.Index % 2 == 0)
            //        {
            //            sideStepSpeed = -sideStepSpeed;
            //        }

            //        float3 dir = math.normalizesafe(-nearest.Ally.Direction);
            //        translation.Value = nearestAllyPosition + dir * UnitRadius * 2.0f + sideStepSpeed * 0.2f;
            //        return;
            //    }
            //}

            //translation.Value = newPosition;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new MovementSystemJob()
        {
            DeltaTime = Time.DeltaTime,
            Tilesize = GameManager.TILE_SIZE,
            UnitRadius = GameManager.TILE_SIZE * 0.2f,
        };

        return job.Schedule(this, inputDependencies);
    }
}