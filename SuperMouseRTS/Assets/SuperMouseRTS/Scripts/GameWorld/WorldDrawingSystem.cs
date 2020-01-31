using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

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

        protected override void OnCreate()
        {
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

        protected override void OnUpdate()
        {
            Entities.ForEach((ref Tile tile, ref TilePosition position) =>
            {
                if (tileMeshes.TryGetValue(tile.tile, out ProcessedMesh[] meshes))
                {
                    var tileOrigin = new Vector3(position.Position.x, 0f, position.Position.y);
                    var tileMatrix = Matrix4x4.Translate(tileOrigin);

                    foreach (var mesh in meshes)
                    {
                        Graphics.DrawMesh(mesh.Mesh, tileMatrix * mesh.Transform, mesh.Material, 0);
                    }
                }

                //if (tilePrefabs.TryGetValue(tile.tile, out GameObject prefab))
                //{
                //    foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                //    {
                //        var meshFilter = renderer.GetComponent<MeshFilter>();
                //        var material = renderer.sharedMaterial;

                //        var prefabMatrix = renderer.GetComponent<Transform>().localToWorldMatrix;
                //        var modelMatrix = Matrix4x4.Translate(new Vector3(position.Position.x, 0f, position.Position.y)) * prefabMatrix;

                //        Graphics.DrawMesh(meshFilter.sharedMesh, modelMatrix, material, 0);
                //    }
                //}
            });
        }
    }
}