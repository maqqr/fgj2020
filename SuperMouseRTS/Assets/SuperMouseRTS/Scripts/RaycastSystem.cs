using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public struct RaycastAABB : IComponentData
{
    public float3 MinBound;
    public float3 MaxBound;
}

public struct RaycastInput
{
    public float3 Origin;
    public float3 Direction; // Not normalized! Includes distance
}

public struct RaycastHit
{
    public bool Hit;
    public float Distance;
    public Entity Entity;
}

public class RaycastSystem : JobComponentSystem
{
    [BurstCompile]
    struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RaycastInput> InputRays;
        public NativeArray<RaycastHit> Results; // Must be same size as InputRays

        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<RaycastAABB> Aabbs; // Must be same size as Entities

        private static float RayBoxIntersect(float3 rpos, float3 rdir, float3 vmin, float3 vmax)
        {
            float t1 = (vmin.x - rpos.x) / rdir.x;
            float t2 = (vmax.x - rpos.x) / rdir.x;
            float t3 = (vmin.y - rpos.y) / rdir.y;
            float t4 = (vmax.y - rpos.y) / rdir.y;
            float t5 = (vmin.z - rpos.z) / rdir.z;
            float t6 = (vmax.z - rpos.z) / rdir.z;

            float aMin = t1 < t2 ? t1 : t2;
            float bMin = t3 < t4 ? t3 : t4;
            float cMin = t5 < t6 ? t5 : t6;

            float aMax = t1 > t2 ? t1 : t2;
            float bMax = t3 > t4 ? t3 : t4;
            float cMax = t5 > t6 ? t5 : t6;

            float fMax = aMin > bMin ? aMin : bMin;
            float fMin = aMax < bMax ? aMax : bMax;

            float t7 = fMax > cMin ? fMax : cMin;
            float t8 = fMin < cMax ? fMin : cMax;

            float t9 = (t8 < 0 || t7 > t8) ? -1.0f : t7;

            return t9;
        }

        public void Execute(int index)
        {
            var ray = InputRays[index];

            RaycastHit hit = new RaycastHit();
            hit.Hit = false;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Entities.Length; i++)
            {
                float distance = RayBoxIntersect(ray.Origin, ray.Direction, Aabbs[i].MinBound, Aabbs[i].MaxBound);

                if (distance > 0.0f && distance < bestDistance)
                {
                    hit.Distance = distance;
                    hit.Hit = true;
                    hit.Entity = Entities[i];
                    bestDistance = distance;
                }
            }

            Results[index] = hit;
        }
    }

    struct CacheJob : IJobForEachWithEntity<RaycastAABB>
    {
        [WriteOnly] public NativeArray<Entity> Entities;
        [WriteOnly] public NativeArray<RaycastAABB> Aabbs;

        public void Execute(Entity entity, int index, ref RaycastAABB aabb)
        {
            Entities[index] = entity;
            Aabbs[index] = aabb;
        }
    }

    private EntityQuery aabbQuery;

    private NativeArray<Entity> entityCache;
    private NativeArray<RaycastAABB> aabbCache;
    private JobHandle cacheJobHandle;

    protected override void OnCreate()
    {
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<RaycastAABB>() },
            Options = EntityQueryOptions.Default,
        };
        aabbQuery = GetEntityQuery(queryDesc);
    }

    protected override void OnDestroy()
    {
        if (aabbCache.IsCreated) aabbCache.Dispose();
        if (entityCache.IsCreated) entityCache.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        if (aabbCache.IsCreated)
        {
            aabbCache.Dispose();
            entityCache.Dispose();
        }

        int aabbEntities = aabbQuery.CalculateEntityCount();

        entityCache = new NativeArray<Entity>(aabbEntities, Allocator.TempJob);
        aabbCache = new NativeArray<RaycastAABB>(aabbEntities, Allocator.TempJob);

        var job = new CacheJob()
        {
            Entities = entityCache,
            Aabbs = aabbCache,
        };

        cacheJobHandle = job.Schedule(this, inputDependencies);
        return cacheJobHandle;
    }

    public bool Raycast(RaycastInput ray, out RaycastHit hit)
    {
        if (!entityCache.IsCreated || !aabbCache.IsCreated)
        {
            UnityEngine.Debug.LogWarning("Attempted to raycast before arrays are ready");
            hit = new RaycastHit();
            return false;
        }

        var raycasts = new NativeArray<RaycastInput>(1, Allocator.TempJob);
        var results = new NativeArray<RaycastHit>(1, Allocator.TempJob);
        raycasts[0] = ray;

        var handle = new RaycastJob()
        {
            InputRays = raycasts,
            Aabbs = aabbCache,
            Entities = entityCache,
            Results = results,
        }.Schedule(raycasts.Length, 5, cacheJobHandle);
        handle.Complete();

        hit = results[0];

        raycasts.Dispose();
        results.Dispose();

        return hit.Hit;
    }

    public void Raycast(RaycastInput[] rays, RaycastHit[] results)
    {
        if (!entityCache.IsCreated || !aabbCache.IsCreated)
        {
            UnityEngine.Debug.LogWarning("Attempted to raycast before arrays are ready");
            return;
        }

        var raycastArray = new NativeArray<RaycastInput>(rays, Allocator.TempJob);
        var resultsArray = new NativeArray<RaycastHit>(rays.Length, Allocator.TempJob);

        var handle = new RaycastJob()
        {
            InputRays = raycastArray,
            Aabbs = aabbCache,
            Entities = entityCache,
            Results = resultsArray,
        }.Schedule(raycastArray.Length, 5);
        handle.Complete();

        System.Array.Resize(ref results, resultsArray.Length);
        resultsArray.CopyTo(results);

        raycastArray.Dispose();
        resultsArray.Dispose();
    }
}