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

    public float TimeBetweenUpdates = 1.0f;
    private float timePassed = 0f;
    private EntityQuery query;

    protected override void OnCreate()
    {
        base.OnCreate();
        query = EntityManager.CreateEntityQuery(typeof(PlayerID), typeof(OreResources), typeof(TilePosition));
    }


    [BurstCompile]
    struct AIDecisionMakerJob : IJobForEach<Translation, UnitTarget, OwnerBuilding>
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<TilePosition> owners;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<TilePosition> targets;

        public void Execute([ReadOnly] ref Translation translation, ref UnitTarget movement, [ReadOnly] ref OwnerBuilding owner)
        {
            for (int i = 0; i < owners.Length; i++)
            {
                if(math.distance(owners[i].Value, owner.owner.Value) < 0.01f)
                {
                    if (movement.Priority <= Priorities.NotUrgent)
                    {
                        movement.Value = targets[i];
                        movement.Operation = AIOperation.Collect;
                        movement.Priority = Priorities.NotUrgent;
                    }
                }
            }
        }
    }



    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        timePassed += Time.DeltaTime;
        if(timePassed < TimeBetweenUpdates)
        {
            return inputDependencies;
        }

        timePassed -= TimeBetweenUpdates;

        var job = new AIDecisionMakerJob();

        NativeArray<TilePosition> owners = query.ToComponentDataArray<TilePosition>(Allocator.TempJob);
        NativeArray<TilePosition> targets = new NativeArray<TilePosition>(owners.Length, Allocator.TempJob);

        for (int i = 0; i < owners.Length; i++)
        {
            targets[i] = FindClosestResource(owners[i]);
        }

        job.owners = owners;
        job.targets = targets;

        inputDependencies = job.Schedule(this, inputDependencies);

        return inputDependencies;
    }

    private TilePosition FindClosestResource(TilePosition pos)
    {
        float closestResult = float.MaxValue;
        TilePosition target = pos; //Cant be unassigned
        Entities.ForEach((ref OreHaulable haul, ref TilePosition resourcePosition) =>{
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