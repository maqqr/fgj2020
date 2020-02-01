using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using System;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    public class WorldDrawingSystem : ComponentSystem
    {
        //[BurstCompile]
        //struct WorldDrawingSystemJob : IJobForEach<Translation, Rotation>
        //{
        //    public void Execute(ref Translation translation, [ReadOnly] ref Rotation rotation)
        //    {
        //    }
        //}

        /// <summary>
        /// This is constructed from the tile's prefab representation.
        /// </summary>
        private struct ProcessedMesh
        {
            public Mesh Mesh;
            public Material Material;
            public Matrix4x4 Transform;
        }

        // This might not be needed anymore
        Dictionary<TileContent, GameObject> tilePrefabs = new Dictionary<TileContent, GameObject>();

        Dictionary<TileContent, ProcessedMesh[]> tileMeshes = new Dictionary<TileContent, ProcessedMesh[]>();

        private WorldStatusSystem worldStatus;


        protected override void OnCreate()
        {
            worldStatus = World.GetOrCreateSystem<WorldStatusSystem>();

            Enabled = GameManager.Instance.IsSettingsLoaded;
            if (Enabled)
            {
                Initialize();
            }

            GameManager.Instance.OnSettingsLoaded += delegate
            {
                Enabled = true;
                Initialize();
            };
        }

        protected override void OnDestroy()
        {
            if (previousFrameTiles.IsCreated)
            {
                previousFrameTiles.Dispose();
            }
        }

        private void Initialize()
        {
            foreach (var pair in GameManager.Instance.LoadedSettings.TilePrefabs)
            {
                tilePrefabs.Add(pair.TileContent, pair.TilePrefab);

                var meshes = new List<ProcessedMesh>();
                foreach (var renderer in pair.TilePrefab.GetComponentsInChildren<MeshRenderer>())
                {
                    meshes.Add(new ProcessedMesh()
                    {
                        Mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                        Material = renderer.sharedMaterial,
                        Transform = renderer.GetComponent<Transform>().localToWorldMatrix,
                    });
                }

                tileMeshes.Add(pair.TileContent, meshes.ToArray());
            }
        }

        private struct MeshMaterialPair
        {
            public Mesh Mesh;
            public Material Material;
            public string Key;

            public MeshMaterialPair(Mesh mesh, Material mat)
            {
                (Mesh, Material) = (mesh, mat);
                Key = mesh.name + mat.name;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MeshMaterialPair))
                {
                    return false;
                }

                var pair = (MeshMaterialPair)obj;
                return Key == pair.Key;
            }

            public override int GetHashCode()
            {
                return 990326508 + EqualityComparer<string>.Default.GetHashCode(Key);
            }

            public static bool operator ==(MeshMaterialPair i1, MeshMaterialPair i2)
            {
                return i1.Key == i2.Key;
            }

            public static bool operator !=(MeshMaterialPair i1, MeshMaterialPair i2)
            {
                return i1.Key != i2.Key;
            }
        }

        bool dictionaryBuilt = false;

        Dictionary<MeshMaterialPair, List<Matrix4x4>> drawList = new Dictionary<MeshMaterialPair, List<Matrix4x4>>();

        NativeArray<Tile> previousFrameTiles;

        protected override void OnUpdate()
        {
            if (!worldStatus.TileCache.IsCreated)
            {
                return;
            }

            // Detect changes in tiles
            if (previousFrameTiles.IsCreated && worldStatus.TileCache.IsCreated)
            {
                for (int i = 0; i < previousFrameTiles.Length; i++)
                {
                    if (previousFrameTiles[i].tile != worldStatus.TileCache[i].tile)
                    {
                        dictionaryBuilt = false;
                    }
                }
            }

            // Copy tiles to previousFrameTiles
            if (previousFrameTiles.IsCreated) previousFrameTiles.Dispose();
            previousFrameTiles = new NativeArray<Tile>(worldStatus.TileCache.Length, Allocator.TempJob);
            worldStatus.TileCache.CopyTo(previousFrameTiles);

            if (!dictionaryBuilt)
            {
                BuildDictionary();
            }

            // Draw tile meshes with instancing
            foreach (var pair in drawList)
            {
                var matrices = pair.Value;

                for (int i = 0; i < matrices.Count; i += 1023)
                {
                    int valuesLeft = Mathf.Min(1023, matrices.Count - i);
                    Matrix4x4[] buffer = new Matrix4x4[valuesLeft];

                    matrices.CopyTo(i, buffer, 0, valuesLeft);

                    Graphics.DrawMeshInstanced(pair.Key.Mesh, 0, pair.Key.Material, buffer);
                }
            }
        }

        private void BuildDictionary()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            dictionaryBuilt = true;
            drawList.Clear();

            Entities.ForEach((Entity entity, ref Tile tile, ref TilePosition position) =>
            {
                if (tileMeshes.TryGetValue(tile.tile, out ProcessedMesh[] meshes))
                {
                    var tileOrigin = WorldCoordinateTools.WorldToUnityCoordinate(position.Value);
                    var tileMatrix = Matrix4x4.Translate(tileOrigin) * Matrix4x4.Scale(new Vector3(GameManager.TILE_SIZE, GameManager.TILE_SIZE, GameManager.TILE_SIZE));

                    foreach (var mesh in meshes)
                    {
                        var matrix = tileMatrix * mesh.Transform;

                        var material = mesh.Material;

                        // Override material in building tiles to give them different colors
                        // TODO: This overrides the ground material also, which is wrong!
                        if (tile.tile == TileContent.Building && EntityManager.HasComponent<PlayerID>(entity))
                        {
                            var materialIndex = EntityManager.GetComponentData<PlayerID>(entity).Value - 1;
                            if (materialIndex >= 0 && materialIndex < GameManager.Instance.LoadedSettings.BuildingMaterials.Length)
                            {
                                material = GameManager.Instance.LoadedSettings.BuildingMaterials[materialIndex];
                            }
                            else Debug.LogWarning($"Invalid materialIndex {materialIndex} when drawing building with custom material");
                        }

                        var key = new MeshMaterialPair(mesh.Mesh, material);

                        if (!drawList.ContainsKey(key))
                        {
                            drawList.Add(key, new List<Matrix4x4>());
                        }
                        drawList[key].Add(matrix);
                    }
                }
            });

            sw.Stop();
            Debug.Log($"Dictionary build time = {sw.Elapsed.TotalMilliseconds:0.0F} ms");
        }
    }
}