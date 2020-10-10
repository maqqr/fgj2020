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

namespace Assets.SuperMouseRTS.Scripts.Players.Units
{

    public class AIDecisionMakerSystem : JobComponentSystem
    {

        public float TimeBetweenUpdates = 1.0f;
        private float timePassed = 0f;
        private EntityQuery FindResourcesQuery;
        private EntityQuery FindAllOwners;

        protected override void OnCreate()
        {
            base.OnCreate();
            FindAllOwners = EntityManager.CreateEntityQuery(typeof(TilePosition), typeof(SpawnScheduler));
            FindResourcesQuery = EntityManager.CreateEntityQuery(typeof(TilePosition), typeof(OreResources), typeof(OreHaulable));
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
                    if (math.distance(owners[i].Value, owner.OwnerTile.Value) < 0.01f)
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

        [BurstCompile]
        struct FindAllNearestJob : IJobForEach<TilePosition, SpawnScheduler>
        {
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<TilePosition> resourceTilePositions;
            [WriteOnly]
            public NativeArray<TilePosition> ownersTargets;
            [WriteOnly]
            public NativeArray<TilePosition> ownerPositions;
            public int ownerIndex;

            public void Execute([ReadOnly] ref TilePosition pos, [ReadOnly] ref SpawnScheduler scheduler)
            {
                float closestResult = float.MaxValue;
                TilePosition target = pos; //Cant be unassigned

                for (int i = 0; i < resourceTilePositions.Length; i++)
                {
                    float distance = math.distance(pos.Value, resourceTilePositions[i].Value);
                    if (distance < closestResult)
                    {
                        closestResult = distance;
                        target = resourceTilePositions[i];
                    }
                }
                ownersTargets[ownerIndex] = target;
                ownerPositions[ownerIndex] = pos;
                ownerIndex++;
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            timePassed += Time.DeltaTime;
            if (timePassed < TimeBetweenUpdates)
            {
                return inputDependencies;
            }

            timePassed -= TimeBetweenUpdates;



            NativeArray<TilePosition> resourcePositions = FindResourcesQuery.ToComponentDataArray<TilePosition>(Allocator.TempJob);

            int ownerCount = FindAllOwners.CalculateEntityCount();
            NativeArray<TilePosition> owners = new NativeArray<TilePosition>(ownerCount, Allocator.TempJob);
            NativeArray<TilePosition> ownerTargets = new NativeArray<TilePosition>(ownerCount, Allocator.TempJob);

            var nearestFinder = new FindAllNearestJob()
            {
                resourceTilePositions = resourcePositions,
                ownersTargets = ownerTargets,
                ownerPositions = owners
            };

            var decisionMakingJob = new AIDecisionMakerJob
            {
                owners = owners,
                targets = ownerTargets
            };

            inputDependencies = nearestFinder.Schedule(this, inputDependencies);
            inputDependencies = decisionMakingJob.Schedule(this, inputDependencies);

            return inputDependencies;
        }
    }
}