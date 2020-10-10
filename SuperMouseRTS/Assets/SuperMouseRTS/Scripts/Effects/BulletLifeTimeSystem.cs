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
    public class BulletLifetimeSystem : SystemBase
    {
        public EndSimulationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

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
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref Bullet bullet) =>
            {
                bullet.LifeTimeLeft -= deltaTime;

                if (bullet.LifeTimeLeft <= 0.0f)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }

            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}