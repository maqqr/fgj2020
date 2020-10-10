using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class GameManagerSystem : ComponentSystem
{
    private GameManager manager;



    protected override void OnCreate()
    {
        manager = new GameManager();
        manager.Initialize();
    }

    protected override void OnUpdate()
    {
    }
}