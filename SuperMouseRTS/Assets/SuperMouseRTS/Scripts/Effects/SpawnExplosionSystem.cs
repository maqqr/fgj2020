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
    public class SpawnExplosionSystem : SystemBase
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
            Settings settings = GameManager.Instance.LoadedSettings;

            Entities.WithAll<ExplosionEvent>().ForEach((Entity entity, in Translation translation) =>
            {
                var obj = GameObject.Instantiate(settings.UnitDeathEffectPrefab, translation.Value, Quaternion.identity);
                obj.transform.localScale = Vector3.one * settings.ExplosionSize;

            }).WithoutBurst().Run();


            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<ExplosionEvent>().ForEach((Entity entity, int entityInQueryIndex) =>
            {
                commandBuffer.DestroyEntity(entityInQueryIndex, entity);

            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}