using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(SpawnSystem))]
public class StresstestSystem : JobComponentSystem
{
    private Settings settings;

    [BurstCompile]
    struct StresstestSystemJob : IJobForEach<SpawnScheduler>
    {

        public void Execute(ref SpawnScheduler sc)
        {
            sc.SpawnsOrdered = 5;
            sc.TimeLeftToSpawn = 0;
        }
    }

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
        settings = GameManager.Instance.LoadedSettings;

        Enabled = true;

    }

    private void OnSettingsLoaded(Settings obj)
    {
        InitializeSystem();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.F9))
        {
            settings.UnitSpawnTime = 0.01f;
            settings.UnitCost = 0;

            var job = new StresstestSystemJob();

            var handle = job.Schedule(this, inputDependencies);
            handle.Complete();
            return inputDependencies;
        }

        return inputDependencies;
    }
}