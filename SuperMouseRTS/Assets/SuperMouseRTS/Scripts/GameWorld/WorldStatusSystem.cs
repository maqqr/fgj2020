﻿using System;
using System.Collections.Generic;
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

        private TileContent this[int x, int y]
        {
            get
            {
                return worldMap[y, x];
            }
            set
            {
                worldMap[y, x] = value;
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

            AssignPlayerStarts(worldMap);

            PopulateWorld(worldMap);

            Enabled = true;
        }

        private void AssignPlayerStarts(TileContent[,] worldMap)
        {
            List<int2> ruinsLocations = new List<int2>();

            Map((tileContent, x, y) =>
            {
                if (tileContent == TileContent.Ruins)
                {
                    ruinsLocations.Add(new int2(x, y));
                }
            });

            
            for (int i = 0; i < settings.Players; i++)
            {
                AssignPlayerBuilding(ruinsLocations);
            }
        }

        private void AssignPlayerBuilding(List<int2> ruinsLocations)
        {
            float distanceCombined = 0;
            Vector3 center = WorldConversionTools.WorldCenter(settings.TilesHorizontally, settings.TilesVertically, settings.TileSize);

            ruinsLocations.ForEach(ruin =>
            {
                distanceCombined += Vector3.Distance(center, WorldConversionTools.WorldToUnityCoordinate(ruin.x, ruin.y, settings.TileSize));
            });


            distanceCombined /= ruinsLocations.Count;

            List<int2> overAverage = ruinsLocations.FindAll((x) => Vector3.Distance(center, WorldConversionTools.WorldToUnityCoordinate(x, settings.TileSize)) > distanceCombined);

            int2 playerLocation = overAverage[UnityEngine.Random.Range(0, overAverage.Count)];

            this[playerLocation.x, playerLocation.y] = TileContent.Building;
            ruinsLocations.Remove(playerLocation);
        }

        private void PopulateWorld(TileContent[,] worldMap)
        {
            int playerID = 1;

            Map((TileContent, x, y) =>
            {
                Entity ent = EntityManager.CreateEntity(typeof(Tile), typeof(TilePosition));
                EntityManager.SetComponentData(ent, new Tile(this[x, y]));
                EntityManager.SetComponentData(ent, new TilePosition(new int2(x, y)));

                if(TileContent == TileContent.Building)
                {
                    PlayerOwner owner = new PlayerOwner(playerID++);
                    if (EntityManager.HasComponent<PlayerOwner>(ent))
                    {
                        EntityManager.SetComponentData<PlayerOwner>(ent, owner);
                    }
                    else
                    {
                        EntityManager.AddComponentData<PlayerOwner>(ent, owner);
                    }
                }
            });
        }

        private void Map(Action<TileContent, int, int> predicate)
        {
            for (int y = 0; y < worldMap.GetLength(0); y++)
            {
                for (int x = 0; x < worldMap.GetLength(1); x++)
                {
                    predicate(this[x, y], x, y);
                }
            }
        }


        private TileContent[,] GenerateWorld()
        {

            int totalTiles = settings.TilesVertically * settings.TilesHorizontally;
            TileContent[,] world = new TileContent[settings.TilesVertically, settings.TilesHorizontally];

            for (int y = 0; y < world.GetLength(0); y++)
            {
                for (int x = 0; x < world.GetLength(1); x++)
                {
                    world[y, x] = TileContent.Empty;
                }
            }

            worldMap = world;

            var rand = new Unity.Mathematics.Random();
            rand.InitState();

            var resourceTiles = settings.PercentileOfTilesResources * 0.01f * totalTiles;
            var ruinsTiles = settings.PercentileOfTilesRuins * 0.01f * totalTiles;
            var obstacles = settings.PercentileOfTilesObstacles * 0.01f * totalTiles;

            TryGenerateTileOfType(rand, resourceTiles, TileContent.Resources, settings.TilesHorizontally, settings.TilesVertically);
            TryGenerateTileOfType(rand, ruinsTiles, TileContent.Ruins, settings.TilesHorizontally, settings.TilesVertically);
            TryGenerateTileOfType(rand, obstacles, TileContent.Obstacle, settings.TilesHorizontally, settings.TilesVertically);

            return world;
        }

        private void TryGenerateTileOfType(Unity.Mathematics.Random rand, float tiles, TileContent tileContent, int tilesHorizontally, int tilesVertically, List<int2> positions = null, int maxTries = 100)
        {
            
            for (int i = 0; i < tiles; i++)
            {
                int2 pos;
                int tries = 0;
                do
                {
                    pos = new int2(rand.NextInt(0, tilesHorizontally), rand.NextInt(0, tilesVertically));
                    tries++;
                }
                while (this[pos.x, pos.y] != TileContent.Empty && tries < maxTries);

                this[pos.x, pos.y] = tileContent;
                if(positions != null)
                {
                    positions.Add(pos);
                }
            }
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

            tileCache = new NativeArray<Tile>(settings.TilesHorizontally * settings.TilesVertically, Allocator.TempJob);

            var job = new WorldGenerationJob();

            job.insertHere = tileCache;
            job.tilesHorizontally = settings.TilesHorizontally;

            // Now that the job is set up, schedule it to be run. 
            latestJobHandle = job.Schedule(this, inputDependencies);

            return latestJobHandle;
        }
    }
}