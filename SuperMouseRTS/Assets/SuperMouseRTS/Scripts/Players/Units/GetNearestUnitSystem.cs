using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class GetNearestUnitSystem : JobComponentSystem
{
    private EntityQuery unitQuery;

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct GetNearestUnitSystemJob : IJobForEach<NearestUnit, Translation, PlayerID>
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> OtherUnits;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Translation> OtherPositions;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<PlayerID> OtherIds;

        public void Execute(ref NearestUnit nearestUnit, [ReadOnly] ref Translation translation, [ReadOnly] ref PlayerID id)
        {
            float bestAllyDist = float.MaxValue;
            float bestEnemyDist = float.MaxValue;

            for (int i = 0; i < OtherPositions.Length; i++)
            {
                bool isAlly = id.Value == OtherIds[i].Value;
                float dist = math.distance(translation.Value, OtherPositions[i].Value);

                if (isAlly)
                {
                    if (dist < bestAllyDist)
                    {
                        nearestUnit.DistToAlly = dist;
                        nearestUnit.NearestAlly = OtherUnits[i];
                    }
                }
                else
                {
                    if (dist < bestEnemyDist)
                    {
                        nearestUnit.DistToEnemy = dist;
                        nearestUnit.NearestEnemy = OtherUnits[i];
                    }
                }
            }
        }
    }

    protected override void OnCreate()
    {
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { new ComponentType(typeof(NearestUnit)), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<PlayerID>() },
            Options = EntityQueryOptions.Default,
        };
        unitQuery = GetEntityQuery(queryDesc);
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var unitEntityArray = unitQuery.ToEntityArray(Allocator.TempJob);
        var positionArray = unitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var playerIdArray = unitQuery.ToComponentDataArray<PlayerID>(Allocator.TempJob);

        var job = new GetNearestUnitSystemJob()
        {
            OtherUnits = unitEntityArray,
            OtherPositions = positionArray,
            OtherIds = playerIdArray,
        };

        return job.Schedule(unitQuery, inputDependencies);
    }
}