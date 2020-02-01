﻿using Assets.SuperMouseRTS.Scripts.GameWorld;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(RaycastSystem))]
public class MouseInputSystem : ComponentSystem
{
    private RaycastSystem raycastSystem;
    private Dictionary<int, Entity> previouslySelectedEntity = new Dictionary<int, Entity>();

    protected override void OnCreate()
    {
        raycastSystem = World.GetOrCreateSystem<RaycastSystem>();
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Player player, ref PlayerID playerId) =>
        {
            var pointerIndex = playerId.Value - 1;
            var pointer = MultiMouse.Instance.GetMouseByIndex(pointerIndex);
            if (pointer == null)
            {
                UnityEngine.Debug.LogWarning($"Mouse pointer {pointerIndex} is null");
                return;
            }

            bool leftClicked = pointer.LeftButtonDown && !player.LeftDownOnLastFrame;
            bool rightClicked = pointer.RightButtonDown && !player.RightDownOnLastFrame;

            if (leftClicked)
            {
                UnityEngine.Debug.Log($"Mouse{pointerIndex} left click");

                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
                var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };

                if (raycastSystem.Raycast(ray, out RaycastHit hit))
                {
                    UnityEngine.Debug.Log("Hit entity " + hit.Entity.Index);

                    BuildingClicked(playerId, hit.Entity);
                }
                else if (previouslySelectedEntity.ContainsKey(pointerIndex))
                {
                    previouslySelectedEntity.Remove(pointerIndex);
                }
            }

            if (rightClicked)
            {
                UnityEngine.Debug.Log($"Mouse{pointerIndex} right click");

                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
                var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };

                if (raycastSystem.Raycast(ray, out RaycastHit hit))
                {
                    UnityEngine.Debug.Log("Right click hit entity " + hit.Entity.Index);

                    BuyUnitsFromBuilding(playerId, hit.Entity);
                }
            }

            player.LeftDownOnLastFrame = pointer.LeftButtonDown;
            player.RightDownOnLastFrame = pointer.RightButtonDown;
        });
    }

    private void BuyUnitsFromBuilding(PlayerID playerId, Entity selectedBuilding)
    {
        var buildingOwnership = EntityManager.GetComponentData<PlayerID>(selectedBuilding);
        bool isOwned = playerId.Value == buildingOwnership.Value;

        if (isOwned)
        {
            var resources = EntityManager.GetComponentData<OreResources>(selectedBuilding);
            var timer = EntityManager.GetComponentData<SpawnScheduler>(selectedBuilding);

            if (resources.Value >= GameManager.Instance.LoadedSettings.UnitCost)
            {
                resources.Value -= GameManager.Instance.LoadedSettings.UnitCost;
                timer.TimeLeftToSpawn = GameManager.Instance.LoadedSettings.UnitSpawnTime;
                timer.SpawnsOrdered++;

                EntityManager.SetComponentData(selectedBuilding, resources);
                EntityManager.SetComponentData(selectedBuilding, timer);
            }
        }
        else
        {
            UnityEngine.Debug.Log("Clicked building is not owned!");
        }

        //Entities.ForEach((ref PlayerID id, ref OreResources resources, ref SpawnTimer timer) =>
        //{
        //    if (timer.TimeLeftToSpawn < 0 && resources.Value >= GameManager.Instance.LoadedSettings.UnitCost)
        //    {
        //        resources.Value -= GameManager.Instance.LoadedSettings.UnitCost;

        //        timer.TimeLeftToSpawn = GameManager.Instance.LoadedSettings.UnitSpawnTime;

        //    }
        //});
    }

    private void BuildingClicked(PlayerID playerId, Entity selectedBuilding)
    {
        // TODO: check if building or ruin

        var pointerIndex = playerId.Value - 1;

        var tile = EntityManager.GetComponentData<Tile>(selectedBuilding);
        var tilePosition = EntityManager.GetComponentData<TilePosition>(selectedBuilding);
        var buildingOwnership = EntityManager.GetComponentData<PlayerID>(selectedBuilding);
        bool isOwned = playerId.Value == buildingOwnership.Value;

        if (isOwned)
        {
            previouslySelectedEntity[pointerIndex] = selectedBuilding;
        }
        else
        {
            if (previouslySelectedEntity.ContainsKey(pointerIndex))
            {
                // TODO: Attack from previouslySelectedEntity[pointerIndex] to selectedBuilding

                previouslySelectedEntity.Remove(pointerIndex);
            }
            else
            {
                // Clicking enemy building first does nothing
            }
        }
    }
}