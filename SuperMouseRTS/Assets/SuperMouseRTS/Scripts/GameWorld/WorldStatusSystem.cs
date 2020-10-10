using System;
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
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class WorldStatusSystem : JobComponentSystem
    {
        private TileContent[,] worldMap;
        private Settings settings;

        private NativeArray<Tile> tileCache;
        private JobHandle latestJobHandle;

        private EntityArchetype buildingArchetype;
        private EndInitializationEntityCommandBufferSystem entityCommandBuffer;
        private EntityQuery allUnitsQuery;

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

        public int TilesHorizontally
        {
            get
            {
                return settings.TilesHorizontally;
            }
        }


        public static Tile FromCache(NativeArray<Tile> tiles, int x, int y, int tilesHorizontally)
        {
            return tiles[x + y * tilesHorizontally];
        }

        public bool IsTileCacheReady
        {
            get
            {
                return tileCache.IsCreated;
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

        public TileContent this[int x, int y]
        {
            get
            {
                return worldMap[y, x];
            }
            private set
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
            entityCommandBuffer = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            buildingArchetype = EntityManager.CreateArchetype(typeof(Tile), typeof(TilePosition));
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

            var queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<OwnerBuilding>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<PlayerID>() },
                Options = EntityQueryOptions.Default,
            };
            allUnitsQuery = GetEntityQuery(queryDesc);

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
            Vector3 center = WorldCoordinateTools.WorldCenter(TilesHorizontally, settings.TilesVertically, GameManager.TILE_SIZE);

            ruinsLocations.ForEach(ruin =>
            {
                distanceCombined += Vector3.Distance(center, WorldCoordinateTools.WorldToUnityCoordinate(ruin.x, ruin.y, GameManager.TILE_SIZE));
            });


            distanceCombined /= ruinsLocations.Count;

            List<int2> overAverage = ruinsLocations.FindAll((x) => Vector3.Distance(center, WorldCoordinateTools.WorldToUnityCoordinate(x, GameManager.TILE_SIZE)) > distanceCombined);

            int2 playerLocation = overAverage[UnityEngine.Random.Range(0, overAverage.Count)];

            this[playerLocation.x, playerLocation.y] = TileContent.Building;
            ruinsLocations.Remove(playerLocation);
        }

        private void PopulateWorld(TileContent[,] worldMap)
        {
            int playerID = 1;

            Map((TileContent, x, y) =>
            {
                var tilePosition = new TilePosition(new int2(x, y));

                Entity ent = EntityManager.CreateEntity(buildingArchetype);
                EntityManager.SetComponentData(ent, new Tile(this[x, y]));
                EntityManager.SetComponentData(ent, tilePosition);

                if (TileContent == TileContent.Building || TileContent == TileContent.Ruins)
                {
                    var size = GameManager.TILE_SIZE / 2.0f;
                    var tileCenter = WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value, GameManager.TILE_SIZE);
                    DOTSTools.SetOrAdd(EntityManager, ent, new RaycastAABB()
                    {
                        MinBound = tileCenter - float3(size, size, size),
                        MaxBound = tileCenter + float3(size, size, size),
                    });

                    var initialHealth = TileContent == TileContent.Ruins ? 0 : 100;
                    DOTSTools.SetOrAdd(EntityManager, ent, new Health(initialHealth, 100));
                }

                if (TileContent == TileContent.Building)
                {
                    DOTSTools.SetOrAdd(EntityManager, ent, new PlayerID(playerID++));
                    DOTSTools.SetOrAdd(EntityManager, ent, new OreResources(settings.StartingResources));
                    DOTSTools.SetOrAdd(EntityManager, ent, new SpawnScheduler(0, -1));
                }

                if (TileContent == TileContent.Resources)
                {
                    DOTSTools.SetOrAdd(EntityManager, ent, new OreResources(settings.ResourceDeposits));
                    DOTSTools.SetOrAdd(EntityManager, ent, new OreHaulable());
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

            int totalTiles = settings.TilesVertically * TilesHorizontally;
            TileContent[,] world = new TileContent[settings.TilesVertically, TilesHorizontally];

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

            TryGenerateTileOfType(rand, ruinsTiles, TileContent.Ruins, TilesHorizontally, settings.TilesVertically);
            TryGenerateTileOfType(rand, resourceTiles, TileContent.Resources, TilesHorizontally, settings.TilesVertically);
            TryGenerateTileOfType(rand, obstacles, TileContent.Obstacle, TilesHorizontally, settings.TilesVertically);

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
                if (positions != null)
                {
                    positions.Add(pos);
                }
            }
        }



        [BurstCompile]
        struct GenerateTileCache : IJobForEachWithEntity<Tile, TilePosition>
        {
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<Tile> insertHere;
            [ReadOnly]
            public int tilesHorizontally;

            public void Execute(Entity ent, int index, [ReadOnly] ref Tile tile, [ReadOnly] ref TilePosition pos)
            {
                insertHere[pos.Value.x + tilesHorizontally * pos.Value.y] = tile;
            }
        }

        [BurstCompile]
        struct RemoveEmptyResourceJob : IJobForEachWithEntity<Tile, TilePosition, OreHaulable, OreResources>
        {
            public EntityCommandBuffer.ParallelWriter entityCommandBuffer;
            public void Execute(Entity ent, int index, ref Tile tile, [ReadOnly] ref TilePosition pos, [ReadOnly] ref OreHaulable tag, ref OreResources res)
            {
                if(res.Value <= 0)
                {
                    tile.tile = TileContent.Empty;
                    entityCommandBuffer.RemoveComponent<OreHaulable>(index, ent);
                    entityCommandBuffer.RemoveComponent<OreResources>(index, ent);
                }
            }
        }


        [BurstCompile]
        struct RepairBuilding : IJobForEachWithEntity<Tile, TilePosition, Health>
        {
            [ReadOnly]
            public NativeArray<Entity> OtherUnits;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<Translation> OtherPositions;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<PlayerID> OtherIds;
            [ReadOnly]
            public int Players;
            [ReadOnly]
            public float TileSize;
            [ReadOnly]
            public int StartingResources;

            public EntityCommandBuffer.ParallelWriter concurrentBuffer;
            public void Execute(Entity ent, int index, ref Tile tile, [ReadOnly] ref TilePosition pos, [ReadOnly] ref Health health)
            {

                if (tile.tile == TileContent.Ruins && health.Value >= health.Maximum)
                {
                    tile.tile = TileContent.Building;
   
                    float3 ruinsUnityLocation = WorldCoordinateTools.UnityCoordinateAsWorld(pos.Value.x, pos.Value.y);

                    NativeArray<int> closeOnesCounts = new NativeArray<int>(Players, Allocator.Temp);

                    for (int i = 0; i < OtherPositions.Length; i++)
                    {
                        if(math.distance(ruinsUnityLocation, OtherPositions[i].Value) <= TileSize)
                        {
                            closeOnesCounts[OtherIds[i].Value - 1]++;
                        }
                    }

                    int winningIndex = 0;
                    for (int i = 0; i < closeOnesCounts.Length; i++)
                    {
                        if(closeOnesCounts[winningIndex] < closeOnesCounts[i])
                        {
                            winningIndex = i;
                        }
                    }
                    concurrentBuffer.AddComponent(index, ent, new PlayerID(winningIndex + 1));
                    concurrentBuffer.AddComponent(index, ent, new OreResources(StartingResources));
                    concurrentBuffer.AddComponent(index, ent, new SpawnScheduler(0, -1));
                    concurrentBuffer.SetComponent(index, ent, new Health(health.Maximum, health.Maximum));
                }
            }
        }


        [BurstCompile]
        struct RuinateBuildingJob : IJobForEachWithEntity<PlayerID, Health, Tile, TilePosition>
        {
            public EntityCommandBuffer.ParallelWriter entityCommandBuffer;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<Entity> PoorPeasants;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<OwnerBuilding> PeasantOwners;

            public void Execute(Entity ent, int index, ref PlayerID id, [ReadOnly] ref Health health, ref Tile tile, [ReadOnly] ref TilePosition pos)
            {
                if(health.Value <= 0)
                {
                    entityCommandBuffer.RemoveComponent<PlayerID>(index, ent);
                    entityCommandBuffer.RemoveComponent<OreResources>(index, ent);
                    entityCommandBuffer.RemoveComponent<SpawnScheduler>(index, ent);
                    entityCommandBuffer.SetComponent<Health>(index, ent, new Health(0, health.Maximum));
                    tile.tile = TileContent.Ruins;

                    for (int i = 0; i < PeasantOwners.Length; i++)
                    {
                        if(math.distance(PeasantOwners[i].owner.Value,pos.Value) < 0.01f)
                        {
                            entityCommandBuffer.DestroyEntity(index, PoorPeasants[i]);
                        }
                    }
                }

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
            //if (!latestJobHandle.IsCompleted)
            //{
            //    latestJobHandle.Complete();
            //}

            tileCache = new NativeArray<Tile>(TilesHorizontally * settings.TilesVertically, Allocator.TempJob);

            var job = new GenerateTileCache()
            {
                insertHere = tileCache,
                tilesHorizontally = TilesHorizontally
            };

            var removeResourcesJob = new RemoveEmptyResourceJob
            {
                entityCommandBuffer = this.entityCommandBuffer.CreateCommandBuffer().AsParallelWriter()
            };

            var unitEntityArray = allUnitsQuery.ToEntityArray(Allocator.TempJob);
            var positionArray = allUnitsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var playerIdArray = allUnitsQuery.ToComponentDataArray<PlayerID>(Allocator.TempJob);
            var owners = allUnitsQuery.ToComponentDataArray<OwnerBuilding>(Allocator.TempJob);

            var repairBuildingJob = new RepairBuilding
            {
                OtherUnits = unitEntityArray,
                OtherPositions = positionArray,
                OtherIds = playerIdArray,
                Players = settings.Players,
                TileSize = GameManager.TILE_SIZE,
                StartingResources = settings.StartingResources,
                concurrentBuffer = this.entityCommandBuffer.CreateCommandBuffer().AsParallelWriter()
            };

            inputDependencies = removeResourcesJob.Schedule(this, inputDependencies);

            inputDependencies = repairBuildingJob.Schedule(this, inputDependencies);

            var ruinateBuildingJob = new RuinateBuildingJob
            {
                PoorPeasants = unitEntityArray,
                PeasantOwners = owners,
                entityCommandBuffer = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter()
            };
            inputDependencies = ruinateBuildingJob.Schedule(this, inputDependencies);

            // Now that the job is set up, schedule it to be run. 
            latestJobHandle = job.Schedule(this, inputDependencies);
            entityCommandBuffer.AddJobHandleForProducer(latestJobHandle);

            return latestJobHandle;
        }
    }
}