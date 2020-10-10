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
using System.Numerics;
using System.CodeDom;

namespace Assets.SuperMouseRTS.Scripts.Effects
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class BulletDrawSystem : SystemBase
    {
        private ComputeBuffer matrixBuffer;

        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        private const int maxBulletCount = 100000;
        private Mesh instanceMesh;

        private EntityQuery query;

        protected override void OnCreate()
        {
            GameManager.Instance.OnSettingsLoaded += Loaded;
            GameManager.Instance.OnSettingsReloaded += Loaded;
            Enabled = false;

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            matrixBuffer = new ComputeBuffer(maxBulletCount, sizeof(float) * 4 * 4);
        }

        private void Loaded(Settings obj)
        {
            Enabled = true;

            Settings settings = GameManager.Instance.LoadedSettings;
            instanceMesh = settings.HealthBarMesh;

            int subMeshIndex = 0;
            args[0] = instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = 0;
            args[2] = instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = instanceMesh.GetBaseVertex(subMeshIndex);
        }

        protected override void OnUpdate()
        {
            const float bulletThickness = 0.05f;
            const float bulletLength = 0.25f;
            Settings settings = GameManager.Instance.LoadedSettings;

            int entityCount = query.CalculateEntityCount();
            NativeArray<UnityEngine.Matrix4x4> bulletMatrices = new NativeArray<UnityEngine.Matrix4x4>(entityCount, Allocator.Temp);


            Entities
                .WithStoreEntityQueryInField(ref query)
                .WithAll<Bullet>().ForEach((int entityInQueryIndex, in Translation translation, in MovementSpeed speed) =>
            {
                if (bulletMatrices.Length < maxBulletCount)
                {
                    float angle = Mathf.Atan2(speed.Value.y, speed.Value.x);

                    UnityEngine.Quaternion rot = UnityEngine.Quaternion.Euler(90.0f, 0.0f, 0.0f)
                                     * UnityEngine.Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * angle);

                    var matrix = UnityEngine.Matrix4x4.Translate(translation.Value + new float3(0.0f, 0.05f, 0.0f))
                                        * UnityEngine.Matrix4x4.Rotate(rot)
                                        * UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(bulletLength, bulletThickness, bulletThickness));

                    bulletMatrices[entityInQueryIndex] = matrix;
                }

            }).Run();

            matrixBuffer.SetData(bulletMatrices);
            settings.BulletMaterial.SetBuffer("matrixBuffer", matrixBuffer);

            args[1] = (uint)bulletMatrices.Length;
            argsBuffer.SetData(args);

            Bounds bounds = new Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(100.0f, 100.0f, 100.0f));

            Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, settings.BulletMaterial, bounds, argsBuffer);

            bulletMatrices.Dispose();
        }
    }
}