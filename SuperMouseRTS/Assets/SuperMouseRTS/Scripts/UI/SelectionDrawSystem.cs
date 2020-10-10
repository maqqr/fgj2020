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
using System.Net;
using Assets.SuperMouseRTS.Scripts.GameWorld;
using Unity.Assertions;

namespace Assets.SuperMouseRTS.Scripts.UI
{
    public static class RandomExtensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            // Available in .NET Core 2.0 and upwards. This method could be moved elsewhere.
            // Required for deconstructing KeyValuePairs in foreach loops.

            key = tuple.Key;
            value = tuple.Value;
        }
    }

    [UpdateAfter(typeof(MouseInputSystem)), AlwaysUpdateSystem]
    public class SelectionDrawSystem : SystemBase
    {
        class SelectionCircle
        {
            public float Size; // Clamped between [0.0, 1.0]
            public bool Active;
            public int CursorIndex;
        }

        Dictionary<Entity, SelectionCircle> selectionCircles = new Dictionary<Entity, SelectionCircle>();

        protected override void OnCreate()
        {
            GameManager.Instance.OnSettingsLoaded += Loaded;
            GameManager.Instance.OnSettingsReloaded += Loaded;
            Enabled = false;
        }

        private void Loaded(Settings obj)
        {
            Enabled = true;
        }

        protected override void OnUpdate()
        {
            Settings settings = GameManager.Instance.LoadedSettings;
            MouseInputSystem mouseInputSystem = World.GetOrCreateSystem<MouseInputSystem>();
            
            HashSet<Entity> selectedBuildingEntities = new HashSet<Entity>();

            // Create circles and find selected entities
            for (int i = 0; i < MultiMouse.Instance.MousePointerCount; i++)
            {
                Entity selectedEntity = mouseInputSystem.GetPreviouslySelectedEntity(i);
                if (selectedEntity != Entity.Null)
                {
                    selectedBuildingEntities.Add(selectedEntity);

                    if (!selectionCircles.ContainsKey(selectedEntity))
                    {
                        selectionCircles.Add(selectedEntity, new SelectionCircle { Size = 0.0f, Active = true, CursorIndex = i });
                    }
                }
            }

            // Update all circles
            List<Entity> removeKeys = new List<Entity>();
            foreach (var (entity, circle) in selectionCircles)
            {
                circle.Active = selectedBuildingEntities.Contains(entity);

                // Handle growing/shrinking
                float growDirection = circle.Active ? 1.0f : -1.0f;
                circle.Size += Time.DeltaTime * settings.CircleGrowSpeed * growDirection;
                circle.Size = Mathf.Clamp(circle.Size, 0.0f, 1.0f);

                // Remove small inactive circles
                if (!circle.Active && circle.Size == 0.0f)
                {
                    removeKeys.Add(entity);
                }
            }
            foreach (var key in removeKeys)
            {
                selectionCircles.Remove(key);
            }

            // Draw circles
            foreach (var (entity, circle) in selectionCircles)
            {
                // TODO: Position needs to be determined in a different way if entity does not have TilePosition
                Assert.IsTrue(EntityManager.HasComponent(entity, typeof(TilePosition)));

                TilePosition tilePosition = EntityManager.GetComponentData<TilePosition>(entity);
                Vector3 unityCoord = WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value) + new float3(0.0f, 0.01f, 0.0f);

                float scale = circle.Size * settings.CircleMaxRadius;

                var matrix = Matrix4x4.Translate(unityCoord + new Vector3(0.0f, 0.05f, 0.0f))
                                * Matrix4x4.Rotate(Quaternion.Euler(90.0f, 0.0f, 0.0f))
                                * Matrix4x4.Scale(Vector3.one * scale);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                Color color = settings.PlayerColors[circle.CursorIndex];
                color.a = circle.Size;
                block.SetColor("_BaseColor", color);

                Graphics.DrawMesh(settings.HealthBarMesh, matrix, settings.circleMaterial, 0, null, 0, block);
            }
        }
    }
}