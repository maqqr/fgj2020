using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class PlayerStatusSystem : JobComponentSystem
{

    protected override void OnCreate()
    {
        base.OnCreate();

        if (!GameManager.Instance.IsSettingsLoaded)
        {
            GameManager.Instance.OnSettingsLoaded += OnSettingsLoaded;
            Enabled = false;
        }
        else
        {
            InitializeSystem();
        }

    }

    private void InitializeSystem()
    {
        Settings settings = GameManager.Instance.LoadedSettings;
        for (int i = 0; i < settings.Players; i++)
        {
            Entity ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData<PlayerID>(ent, new PlayerID(i + 1));
            EntityManager.AddComponentData<OreResources>(ent, new OreResources(settings.StartingResources));
        }
    }

    private void OnSettingsLoaded(Settings obj)
    {
        InitializeSystem();
    }

    [BurstCompile]
    struct PlayerStatusSystemJob : IJobForEach<Translation, Rotation>
    {        
        
        public void Execute(ref Translation translation, [ReadOnly] ref Rotation rotation)
        {
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as [ReadOnly], which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            
            
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new PlayerStatusSystemJob();
        
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;
        
        
        
        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);
    }
}