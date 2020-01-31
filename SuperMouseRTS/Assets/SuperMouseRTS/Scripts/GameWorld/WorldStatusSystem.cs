using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    public class WorldStatusSystem : JobComponentSystem
    {
        private TileContent[,] worldMap;

        private NativeArray<Tile> tileCache;
        private Settings settings;


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
                this.settings = GameManager.Instance.LoadedSettings;
                InitializeSystem();
            }
        }


        private void OnSettingsLoaded(Settings settings)
        {
            this.settings = settings;

            InitializeSystem();
        }

        private void InitializeSystem()
        {
            worldMap = GenerateWorld();

            PopulateWorld(worldMap);

            Enabled = true;
        }

        private void PopulateWorld(TileContent[,] worldMap)
        {
            for (int i = 0; i < worldMap.GetLength(0); i++)
            {
                for (int j = 0; j < worldMap.GetLength(1); j++)
                {
                    Entity ent = EntityManager.CreateEntity(typeof(Tile), typeof(TilePosition));
                    EntityManager.SetComponentData(ent, new Tile(worldMap[i, j]));
                    EntityManager.SetComponentData(ent, new TilePosition(new int2(j, i)));
                }
            }

        }

        private TileContent[,] GenerateWorld()
        {
            TileContent[,] world = new TileContent[settings.tilesVertically, settings.tilesHorizontally];

            for (int i = 0; i < world.GetLength(0); i++)
            {
                for (int j = 0; j < world.GetLength(1); j++)
                {
                    world[i, j] = TileContent.Empty;
                }
            }

            return world;
        }


        // This declares a new kind of job, which is a unit of work to do.
        // The job is declared as an IJobForEach<Translation, Rotation>,
        // meaning it will process all entities in the world that have both
        // Translation and Rotation components. Change it to process the component
        // types you want.
        //
        // The job is also tagged with the BurstCompile attribute, which means
        // that the Burst compiler will optimize it for the best performance.
        [BurstCompile]
        struct WorldGenerationJob : IJobForEachWithEntity<Tile, TilePosition>
        {
            [WriteOnly]
            public NativeArray<Tile> insertHere;
            [ReadOnly]
            public int tilesHorizontally;


            public void Execute(Entity ent, int index, [ReadOnly] ref Tile tile, [ReadOnly] ref TilePosition pos)
            {
                insertHere[pos.Position.x + tilesHorizontally * pos.Position.y] = tile;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            if (tileCache.IsCreated)
            {
                tileCache.Dispose();
            }
            tileCache = new NativeArray<Tile>(settings.tilesHorizontally * settings.tilesVertically, Allocator.TempJob);

            var job = new WorldGenerationJob();

            job.insertHere = tileCache;
            job.tilesHorizontally = settings.tilesHorizontally;

            // Now that the job is set up, schedule it to be run. 
            return job.Schedule(this, inputDependencies);
        }
    }
}