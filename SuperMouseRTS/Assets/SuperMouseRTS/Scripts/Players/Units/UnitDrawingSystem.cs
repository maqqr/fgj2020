using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class UnitDrawingSystem : ComponentSystem
{
    private Mesh unitMesh;
    private Material[] unitMaterials;
    //private EntityQuery unitQuery;
    private const float unitScale = 0.05f * GameManager.TILE_SIZE;

    protected override void OnCreate()
    {
        GameManager.Instance.OnSettingsLoaded += Loaded;
        Enabled = false;

        //var queryDesc = new EntityQueryDesc
        //{
        //    All = new ComponentType[]
        //    {
        //        ComponentType.ReadOnly<Translation>(),
        //        ComponentType.ReadOnly<Rotation>(),
        //        ComponentType.ReadOnly<PlayerID>()
        //    },
        //    Options = EntityQueryOptions.Default,
        //};
        //unitQuery = GetEntityQuery(queryDesc);
    }

    private void Loaded(Settings obj)
    {
        Enabled = true;

        var prefab = GameManager.Instance.LoadedSettings.UnitPrefab;
        unitMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        unitMaterials = GameManager.Instance.LoadedSettings.UnitMaterials;
    }

    protected override void OnUpdate()
    {
        Dictionary<int, List<Matrix4x4>> unitModels = new Dictionary<int, List<Matrix4x4>>();

        Entities.ForEach((ref Translation translation, ref Rotation rotation, ref PlayerID playerId) =>
        {
            var matrix = Matrix4x4.Translate(translation.Value)
                         * Matrix4x4.Rotate(rotation.Value)
                         * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0f, 0f))
                         * Matrix4x4.Scale(new Vector3(unitScale, unitScale, unitScale));

            var id = playerId.Value;
            if (!unitModels.ContainsKey(id))
            {
                unitModels.Add(id, new List<Matrix4x4>());
            }
            unitModels[id].Add(matrix);
        });

        foreach(var keyValue in unitModels)
        {
            var materialIndex = keyValue.Key - 1;
            if (!(materialIndex >= 0 && materialIndex < unitMaterials.Length))
            {
                Debug.LogWarning($"Failed to draw unit: index {materialIndex} outside of unitMaterials array");
                continue;
            }
            var material = unitMaterials[materialIndex];
            var modelList = keyValue.Value;
            Matrix4x4[] matrices = new Matrix4x4[modelList.Count];
            keyValue.Value.CopyTo(0, matrices, 0, modelList.Count);

            Graphics.DrawMeshInstanced(unitMesh, 0, material, matrices);
        }

    }
}