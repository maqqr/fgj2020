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
using UnityEngine.Rendering;

namespace Assets.SuperMouseRTS.Scripts.UI
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class HealthDrawSystem : SystemBase
    {
        private Mesh healthBarMesh;
        private Material[] healthBarMaterials;

        List<Matrix4x4> matrices = new List<Matrix4x4>();

        protected override void OnCreate()
        {
            GameManager.Instance.OnSettingsLoaded += Loaded;
            GameManager.Instance.OnSettingsReloaded += Loaded;
            Enabled = false;
        }

        private void Loaded(Settings obj)
        {
            Enabled = true;

            healthBarMesh = GameManager.Instance.LoadedSettings.HealthBarMesh;
            healthBarMaterials = GameManager.Instance.LoadedSettings.HealthBarMaterials;
        }

        private Matrix4x4 makeBar(Vector3 worldSpacePosition, float width)
        {
            Vector3 healthBarRot = GameManager.Instance.LoadedSettings.HealthBarRotation;

            return Matrix4x4.Translate(worldSpacePosition)
                   * Matrix4x4.Rotate(Quaternion.Euler(healthBarRot.x, healthBarRot.y, healthBarRot.z))
                   * Matrix4x4.Scale(new Vector3(width, 0.1f, 0.1f));
        }

        void addBar(Dictionary<int, List<Matrix4x4>> hpBarMatrices, in PlayerID playerId, in Matrix4x4 matrix)
        {
            var id = playerId.Value;
            if (!hpBarMatrices.ContainsKey(id))
            {
                hpBarMatrices.Add(id, new List<Matrix4x4>());
            }
            hpBarMatrices[id].Add(matrix);
        }

        protected override void OnUpdate()
        {
            bool showHpAlways = GameManager.Instance.LoadedSettings.ShowHpAlways;
            float healthBarY = GameManager.Instance.LoadedSettings.UnitHealthBarY;
            float buildingHealthBarY = GameManager.Instance.LoadedSettings.BuildingHealthBarY;
            float unitHealthBarWidth = GameManager.Instance.LoadedSettings.UnitHealthBarWidth;
            float buildingHealthBarWidth = GameManager.Instance.LoadedSettings.BuildingHealthBarWidth;

            float hpAmount = Mathf.Sin((float)Time.ElapsedTime);

            Dictionary<int, List<Matrix4x4>> hpBarMatrices = new Dictionary<int, List<Matrix4x4>>();

            // Get hp bar from units
            Entities.ForEach((in Translation translation, in Health health, in PlayerID playerId) =>
            {
                if (showHpAlways || health.Value < health.Maximum)
                {
                    var width = (health.Value / (float)health.Maximum) * unitHealthBarWidth;
                    var barMatrix = makeBar(translation.Value + float3(0f, healthBarY, 0.0f), width);
                    addBar(hpBarMatrices, playerId, barMatrix);
                }

            }).WithoutBurst().Run();

            // Get hp bar from owned buildings
            Entities.WithAll<Tile>().ForEach((in TilePosition tilePosition, in Health health, in PlayerID playerId) =>
            {
                if (showHpAlways || health.Value < health.Maximum)
                {
                    var position = WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value);

                    var width = (health.Value / (float)health.Maximum) * buildingHealthBarWidth;
                    var barMatrix = makeBar(position + float3(0f, buildingHealthBarY, 0.0f), width);
                    addBar(hpBarMatrices, playerId, barMatrix);
                }

            }).WithoutBurst().Run();

            // Get hp bar from unbuilt buildings
            Entities.WithAll<Tile>().WithNone<PlayerID>().ForEach((in TilePosition tilePosition, in Health health) =>
            {
                if (showHpAlways || health.Value < health.Maximum)
                {
                    var position = WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value);

                    var width = (health.Value / (float)health.Maximum) * buildingHealthBarWidth;
                    var barMatrix = makeBar(position + float3(0f, buildingHealthBarY, 0.0f), width);

                    var unowned = new PlayerID { Value = 0 };
                    addBar(hpBarMatrices, unowned, barMatrix);
                }

            }).WithoutBurst().Run();

            foreach (var keyValue in hpBarMatrices)
            {
                var materialIndex = keyValue.Key;
                if (!(materialIndex >= 0 && materialIndex < healthBarMaterials.Length))
                {
                    Debug.LogWarning($"Failed to draw hp bar: index {materialIndex} outside of healthBarMaterials array");
                    continue;
                }
                var material = healthBarMaterials[materialIndex];
                var matrices = keyValue.Value;

                for (int i = 0; i < matrices.Count; i += 1023)
                {
                    int valuesLeft = Mathf.Min(1023, matrices.Count - i);
                    Matrix4x4[] buffer = new Matrix4x4[valuesLeft];
                    keyValue.Value.CopyTo(i, buffer, 0, valuesLeft);

                    Graphics.DrawMeshInstanced(healthBarMesh, 0, material, buffer, buffer.Length, null, ShadowCastingMode.Off, false);
                }
            }
        }
    }
}