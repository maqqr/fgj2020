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
using System.ComponentModel;

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

        private Vector3 GetSelectedEntityPosition(Entity entity)
        {
            if (EntityManager.HasComponent(entity, typeof(TilePosition)))
            {
                TilePosition tilePosition = EntityManager.GetComponentData<TilePosition>(entity);
                return WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value) + new float3(0.0f, 0.01f, 0.0f);
            }
            if (EntityManager.HasComponent(entity, typeof(Translation)))
            {
                return EntityManager.GetComponentData<Translation>(entity).Value;
            }

            throw new UnityException($"{nameof(SelectionDrawSystem)}.{nameof(GetSelectedEntityPosition)} needs fixing");
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float thickness)
        {
            Settings settings = GameManager.Instance.LoadedSettings;

            Vector3 lineDir = end - start;
            Vector3 middle = start + lineDir * 0.5f;
            float lineLength = lineDir.magnitude;


            float angle = Mathf.Atan2(lineDir.z, lineDir.x);

            Quaternion rot = Quaternion.Euler(90.0f, 0.0f, 0.0f)
                             * Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * angle);

            var matrix = Matrix4x4.Translate(middle + new Vector3(0.0f, 0.05f, 0.0f))
                                * Matrix4x4.Rotate(rot)
                                * Matrix4x4.Scale(new Vector3(lineLength, thickness, thickness));

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);

            Graphics.DrawMesh(settings.HealthBarMesh, matrix, settings.LineMaterial, 0, null, 0, block);
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

            // Draw arrow from selected building to mouse
            foreach (var pointer in MultiMouse.Instance.GetMousePointers())
            {
                Entity selectedEntity = mouseInputSystem.GetPreviouslySelectedEntity(pointer.PlayerIndex);
                if (!selectionCircles.ContainsKey(selectedEntity))
                    continue;

                SelectionCircle circle = selectionCircles[selectedEntity];
                Color lineColor = settings.PlayerColors[pointer.PlayerIndex];

                Plane ground = new Plane(Vector3.up, Vector3.zero);
                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);

                Vector3 start = GetSelectedEntityPosition(selectedEntity);

                if (!ground.Raycast(unityRay, out float rayDistance))
                    continue;

                Vector3 end = unityRay.GetPoint(rayDistance);
                Vector3 lineDir = end - start;

                // Shift start vector a bit to prevent intersecting the selected building's circle
                start += lineDir.normalized * circle.Size * settings.CircleMaxRadius * 0.5f;

                // Draw line from building to cursor
                DrawLine(start, end, lineColor, 0.1f);

                // Draw the arrow head
                float angle = Mathf.Atan2(lineDir.z, lineDir.x);
                float offset = 15.0f;
                Vector3 headEnd1 = end + settings.ArrowHeadLength * new Vector3(Mathf.Cos(angle + offset), 0.0f, Mathf.Sin(angle + offset));
                Vector3 headEnd2 = end + settings.ArrowHeadLength * new Vector3(Mathf.Cos(angle - offset), 0.0f, Mathf.Sin(angle - offset));
                DrawLine(end, headEnd1, lineColor, 0.1f);
                DrawLine(end, headEnd2, lineColor, 0.1f);
            }
        }
    }
}