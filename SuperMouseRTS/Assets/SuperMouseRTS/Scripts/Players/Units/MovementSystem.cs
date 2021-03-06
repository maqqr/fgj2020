﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;


public class MovementSystem : JobComponentSystem
{
    [BurstCompile]
    struct MovementSystemJob : IJobForEachWithEntity<Translation, Rotation, MovementSpeed, NearestUnit>
    {
        public float Tilesize;
        public float DeltaTime;
        public float UnitRadius;

        public void Execute(Entity ent, int index, ref Translation translation, ref Rotation rotation, [ReadOnly] ref MovementSpeed speed, [ReadOnly] ref NearestUnit nearest)
        {
            translation.Value += WorldCoordinateTools.UnityCoordinateAsWorld(speed.Value.x, speed.Value.y) * DeltaTime; ;

            float angle = atan2(speed.Value.y, speed.Value.x) - PI * 0.5f;
            rotation.Value = EulerAngle(0f, angle, 0f);
        }

        public quaternion EulerAngle(float yaw, float pitch, float roll) // yaw (Z), pitch (Y), roll (X)
        {
            float cy = cos(yaw * 0.5f);
            float sy = sin(yaw * 0.5f);
            float cp = cos(pitch * 0.5f);
            float sp = sin(pitch * 0.5f);
            float cr = cos(roll * 0.5f);
            float sr = sin(roll * 0.5f);

            var q = new quaternion();
            q.value.w = cy * cp * cr + sy * sp * sr;
            q.value.x = cy * cp * sr - sy * sp * cr;
            q.value.y = sy * cp * sr + cy * sp * cr;
            q.value.z = sy * cp * cr - cy * sp * sr;

            return q;
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