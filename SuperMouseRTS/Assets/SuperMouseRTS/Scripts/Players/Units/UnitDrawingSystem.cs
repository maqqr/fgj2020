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
    private Material unitMaterial;

    private const float unitScale = 0.05f * GameManager.TILE_SIZE;

    protected override void OnCreate()
    {
        GameManager.Instance.OnSettingsLoaded += Loaded;
        Enabled = false;
    }

    private void Loaded(Settings obj)
    {
        Enabled = true;

        var prefab = GameManager.Instance.LoadedSettings.UnitPrefab;
        unitMaterial = prefab.GetComponent<MeshRenderer>().sharedMaterial;
        unitMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation, ref Rotation rotation, ref PlayerID playerId) =>
        {
            var matrix = Matrix4x4.Translate(translation.Value)
                         * Matrix4x4.Rotate(rotation.Value)
                         * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0f, 0f))
                         * Matrix4x4.Scale(new Vector3(unitScale, unitScale, unitScale));

            Graphics.DrawMesh(unitMesh, matrix, unitMaterial, 0);
        });
    }
}