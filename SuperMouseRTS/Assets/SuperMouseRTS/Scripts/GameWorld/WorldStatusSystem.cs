using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class WorldStatusSystem : JobComponentSystem
    {
        private TileContent[,] worldMap;
        private Settings settings;

        private NativeArray<Tile> tileCache;
        private JobHandle latestJobHandle;

        public NativeArray<Tile> TileCache
        {
            get
            {
                if (!tileCache.IsCreated)
                {
                    throw new UnityException("Tile cache was not created before!");
                }
                return tileCache;
            }
        }
        public JobHandle LatestJobHandle
        {
            get
            {
                return latestJobHandle;
            }
            set
            {
                latestJobHandle = value;
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


        protected override void OnDestroy()
        {
            base.OnDestroy();
            TryDisposeCache();
        }

        private void TryDisposeCache()
        {
            if (tileCache.IsCreated)
            {
                tileCache.Dispose();
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            TryDisposeCache();
            if (!latestJobHandle.IsCompleted)
            {
                latestJobHandle.Complete();
            }

            tileCache = new NativeArray<Tile>(settings.tilesHorizontally * settings.tilesVertically, Allocator.TempJob);

            var job = new WorldGenerationJob();

            job.insertHere = tileCache;
            job.tilesHorizontally = settings.tilesHorizontally;

            // Now that the job is set up, schedule it to be run. 
            latestJobHandle = job.Schedule(this, inputDependencies);

            return latestJobHandle;
        }
    }
}