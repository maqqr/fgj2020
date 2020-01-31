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

        Dictionary<TileContent, GameObject> tilePrefabs = new Dictionary<TileContent, GameObject>();

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
            }
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref Tile tile, ref TilePosition position) =>
            {
                if (tilePrefabs.TryGetValue(tile.tile, out GameObject prefab))
                {
                    foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                    {
                        var meshFilter = renderer.GetComponent<MeshFilter>();
                        var material = renderer.sharedMaterial;

                        Vector3 localPosition = renderer.GetComponent<Transform>().localPosition;
                        Vector3 tileOrigin = new Vector3(position.Position.x, 0f, position.Position.y);

                        Vector3 finalPosition = tileOrigin + localPosition;
                        var modelMatrix = Matrix4x4.Translate(finalPosition);

                        Graphics.DrawMesh(meshFilter.sharedMesh, modelMatrix, material, 0);
                    }
                }
            });
        }
    }
}