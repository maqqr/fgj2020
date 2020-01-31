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

        Dictionary<MeshMaterialPair, Matrix4x4[]> drawList = new Dictionary<MeshMaterialPair, Matrix4x4[]>();

        protected override void OnUpdate()
        {
            if (!dictionaryBuilt)
            {
                dictionaryBuilt = true;

                Entities.ForEach((ref Tile tile, ref TilePosition position) =>
                {
                    if (tileMeshes.TryGetValue(tile.tile, out ProcessedMesh[] meshes))
                    {
                        var tileOrigin = new Vector3(position.Position.x, 0f, position.Position.y);
                        var tileMatrix = Matrix4x4.Translate(tileOrigin);

                        foreach (var mesh in meshes)
                        {
                            var matrix = tileMatrix * mesh.Transform;
                            //Graphics.DrawMesh(mesh.Mesh, , mesh.Material, 0);

                            var key = new MeshMaterialPair(mesh.Mesh, mesh.Material);

                            if (drawList.ContainsKey(key))
                            {
                                var arr = drawList[key];
                                System.Array.Resize(ref arr, arr.Length + 1);
                                arr[arr.Length - 1] = matrix;
                                drawList[key] = arr;
                            }
                            else
                            {
                                drawList.Add(key, new Matrix4x4[] { matrix });
                            }
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

            foreach (var pair in drawList)
            {
                var matrices = pair.Value;

                for (int i = 0; i < matrices.Length; i += 1023)
                {
                    int valuesLeft = Mathf.Min(1023, matrices.Length - i);
                    Matrix4x4[] buffer = new Matrix4x4[valuesLeft];
                    System.Array.Copy(pair.Value, i, buffer, 0, valuesLeft);
                    Graphics.DrawMeshInstanced(pair.Key.Mesh, 0, pair.Key.Material, buffer);
                }
            }
        }
    }
}