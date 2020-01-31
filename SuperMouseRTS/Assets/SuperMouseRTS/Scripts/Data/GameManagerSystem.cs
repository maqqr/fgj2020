using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class GameManagerSystem : ComponentSystem
{
    private GameManager manager;

    protected override void OnCreate()
    {
        base.OnCreateManager();
        manager = new GameManager();
        manager.Initialize();
    }

    protected override void OnUpdate()
    {
    }
}