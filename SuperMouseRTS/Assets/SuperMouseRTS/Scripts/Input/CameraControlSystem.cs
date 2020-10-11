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

namespace Assets.SuperMouseRTS.Scripts.Input
{

    [Serializable]
    public struct Camera : IComponentData
    {
        public float DistanceToGround;
    }

    public class CameraControlSystem : SystemBase
    {
        public EndSimulationEntityCommandBufferSystem commandBufferSystem;

        public Dictionary<KeyCode, float2> moveKeyBinds = new Dictionary<KeyCode, float2>
        {
            { KeyCode.RightArrow, new float2(-1.0f, 0.0f) },
            { KeyCode.LeftArrow, new float2(1.0f, 0.0f) },
            { KeyCode.UpArrow, new float2(0.0f, -1.0f) },
            { KeyCode.DownArrow, new float2(0.0f, 1.0f) },
        };

        public Dictionary<KeyCode, float> zoomKeyBinds = new Dictionary<KeyCode, float>
        {
            { KeyCode.KeypadPlus, -1.0f },
            { KeyCode.KeypadMinus, 1.0f },
        };

        Entity cameraEntity = Entity.Null;

        protected override void OnCreate()
        {
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            GameManager.Instance.OnSettingsLoaded += Loaded;
            GameManager.Instance.OnSettingsReloaded += Loaded;
            Enabled = false;

            cameraEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<Camera>(cameraEntity);
            EntityManager.AddComponent<MovementSpeed>(cameraEntity);
            EntityManager.AddComponent<Translation>(cameraEntity);

            EntityManager.SetComponentData(cameraEntity, new Camera() { DistanceToGround = 24.0f });
            EntityManager.SetComponentData(cameraEntity, new Translation() { Value = float3(15.0f, 0.0f, 17.0f) });
        }

        private void Loaded(Settings obj)
        {
            Enabled = true;
        }

        protected override void OnUpdate()
        {
            Settings settings = GameManager.Instance.LoadedSettings;
            float deltaTime = Time.DeltaTime;

            Entities.ForEach((ref Translation translation, ref MovementSpeed speed, ref Camera camera) =>
            {
                // Camera movement
                float2 acceleration = float2(0, 0);
                foreach (var (keyCode, moveVector) in moveKeyBinds)
                {
                    if (UnityEngine.Input.GetKey(keyCode))
                    {
                        acceleration += moveVector;
                    }
                }

                if (math.length(acceleration) > 1.0f)
                {
                    acceleration = math.normalize(acceleration);
                }
                acceleration *= settings.CameraMoveSpeed;

                speed.Value += acceleration * deltaTime;
                speed.Value += -speed.Value * settings.CameraDamping * deltaTime;

                if (math.length(speed.Value) > settings.CameraMaxMoveSpeed)
                {
                    speed.Value = math.normalize(speed.Value) * settings.CameraMaxMoveSpeed;
                }
                translation.Value += float3(speed.Value.x, 0.0f, speed.Value.y) * deltaTime;

                // Camera zoom
                float zoomSpeed = 0.0f;
                foreach (var (keyCode, zoomDir) in zoomKeyBinds)
                {
                    if (UnityEngine.Input.GetKey(keyCode))
                    {
                        zoomSpeed += zoomDir * settings.CameraZoomSpeed;
                    }
                }

                Debug.Log(UnityEngine.Input.mouseScrollDelta);

                zoomSpeed += UnityEngine.Input.mouseScrollDelta.y * zoomSpeed;

                camera.DistanceToGround += zoomSpeed * deltaTime;

                camera.DistanceToGround = math.clamp(camera.DistanceToGround, 3.0f, 30.0f);

            }).WithoutBurst().Run();

            // Sync camera with Unity camera
            if (cameraEntity != Entity.Null)
            {
                Translation translation = EntityManager.GetComponentData<Translation>(cameraEntity);
                Camera cameraData = EntityManager.GetComponentData<Camera>(cameraEntity);

                UnityEngine.Camera.main.transform.position =
                    (Vector3)translation.Value + UnityEngine.Camera.main.transform.forward * -cameraData.DistanceToGround;
            }
        }
    }
}