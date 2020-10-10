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

namespace Assets.SuperMouseRTS.Scripts.Effects
{
    public class BulletMovementSystem : SystemBase
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
            float deltaTime = Time.DeltaTime;

            Entities.WithAll<Bullet>().ForEach((ref Translation translation, in MovementSpeed speed) =>
            {
                translation.Value.x += speed.Value.x * deltaTime;
                translation.Value.z += speed.Value.y * deltaTime;

            }).ScheduleParallel();
        }
    }
}