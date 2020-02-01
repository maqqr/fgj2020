using System;
using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class AIDecisionMakerSystem : JobComponentSystem
{
    [BurstCompile]
    struct AIDecisionMakerJob : IJobForEach<Translation, MovementSpeed, OwnerBuilding>
    {
        [ReadOnly]
        public NativeArray<TilePosition> owners;
        [ReadOnly]
        public NativeArray<TilePosition> targets;
        [ReadOnly]
        public float speed;
        
        public void Execute([ReadOnly] ref Translation translation, ref MovementSpeed movement, [ReadOnly] ref OwnerBuilding owner)
        {
            for (int i = 0; i < owners.Length; i++)
            {
                if(math.distance(owners[i].Value, owner.owner.Value) < 0.01f)
                {
                    float2 vec = new float2(targets[i].Value.x * GameManager.TILE_SIZE - translation.Value.x, targets[i].Value.y * GameManager.TILE_SIZE - translation.Value.z);
                    movement.Value = math.normalize(vec) * speed;
                }
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new AIDecisionMakerJob();

        EntityQuery query = EntityManager.CreateEntityQuery(typeof(PlayerID), typeof(OreResources), typeof(TilePosition));
        NativeArray<TilePosition> owners = query.ToComponentDataArray<TilePosition>(Allocator.TempJob);
        NativeArray<TilePosition> targets = new NativeArray<TilePosition>(owners.Length, Allocator.TempJob);

        for (int i = 0; i < owners.Length; i++)
        {
            targets[i] = FindClosestResource(owners[i]);
        }

        job.owners = owners;
        job.targets = targets; //TODO: dispose these?
        job.speed = 5f;

        inputDependencies = job.Schedule(this, inputDependencies);
        inputDependencies.Complete();

        owners.Dispose();
        targets.Dispose();

        return inputDependencies;
    }

    private TilePosition FindClosestResource(TilePosition pos)
    {
        float closestResult = float.MaxValue;
        TilePosition target = pos; //Cant be unassigned
        Entities.ForEach((ref OreHaulingSpeed haul, ref TilePosition resourcePosition) =>{
            float result = math.distancesq(pos.Value, resourcePosition.Value);
            if(result < closestResult)
            {
                closestResult = result;
                target = resourcePosition;
            }
        }).Run();

        return target;
    }
}