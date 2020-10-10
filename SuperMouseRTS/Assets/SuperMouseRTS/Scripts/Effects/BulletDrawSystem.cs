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

namespace Assets.SuperMouseRTS.Scripts.Effects
{
    public class BulletDrawSystem : SystemBase
    {
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
            //List<Matrix4x4> bulletMatrices = new List<Matrix4x4>();

            NativeList<UnityEngine.Matrix4x4> bulletMatrices = new NativeList<UnityEngine.Matrix4x4>(Allocator.Temp);

            Entities.WithAll<Bullet>().ForEach((in Translation translation, in MovementSpeed speed) =>
            {
                float angle = Mathf.Atan2(speed.Value.y, speed.Value.x);

                UnityEngine.Quaternion rot = UnityEngine.Quaternion.Euler(90.0f, 0.0f, 0.0f)
                                 * UnityEngine.Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * angle);

                var matrix = UnityEngine.Matrix4x4.Translate(translation.Value + new float3(0.0f, 0.05f, 0.0f))
                                    * UnityEngine.Matrix4x4.Rotate(rot)
                                    * UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(0.3f, 0.1f, 0.1f));

                bulletMatrices.Add(matrix);


            }).Run();

            //for (int i = 0; i < bulletMatrices.Length; i += 1023)
            //{
            //    int valuesLeft = Mathf.Min(1023, bulletMatrices.Length - i);
            //    UnityEngine.Matrix4x4[] buffer = new UnityEngine.Matrix4x4[valuesLeft];
            //    bulletMatrices.CopyTo(i, buffer, 0, valuesLeft);

            //    Graphics.DrawMeshInstanced(settings.HealthBarMesh, 0, settings.LineMaterial, bulletMatrices.AsArray(), buffer.Length, null, ShadowCastingMode.Off, false);
            //}
        }
    }
}