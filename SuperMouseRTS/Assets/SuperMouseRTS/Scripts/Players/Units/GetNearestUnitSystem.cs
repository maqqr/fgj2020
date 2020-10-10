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

    [BurstCompile]
    struct GetNearestUnitSystemJob : IJobForEachWithEntity<NearestUnit, Translation, PlayerID>
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> OtherUnits;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Translation> OtherPositions;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<PlayerID> OtherIds;

        public void Execute(Entity ent, int index, ref NearestUnit nearestUnit, [ReadOnly] ref Translation translation, [ReadOnly] ref PlayerID id)
        {
            float bestAllyDist = float.MaxValue;
            float bestEnemyDist = float.MaxValue;

            nearestUnit.Enemy.Direction = float3(0f);
            nearestUnit.Ally.Direction = float3(0f);

            for (int i = 0; i < OtherPositions.Length; i++)
            {
                if (ent.Index == OtherUnits[i].Index)
                    continue;

                bool isAlly = id.Value == OtherIds[i].Value;
                float dist = math.distance(translation.Value, OtherPositions[i].Value);

                if (isAlly)
                {
                    if (dist < bestAllyDist)
                    {
                        nearestUnit.Ally.Entity = OtherUnits[i];
                        nearestUnit.Ally.Direction = OtherPositions[i].Value - translation.Value;
                        nearestUnit.Ally.Position = OtherPositions[i].Value;
                        bestAllyDist = dist;
                    }
                }
                else
                {
                    if (dist < bestEnemyDist)
                    {
                        nearestUnit.Enemy.Entity = OtherUnits[i];
                        nearestUnit.Enemy.Direction = OtherPositions[i].Value - translation.Value;
                        nearestUnit.Enemy.Position = OtherPositions[i].Value;
                        bestEnemyDist = dist;
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