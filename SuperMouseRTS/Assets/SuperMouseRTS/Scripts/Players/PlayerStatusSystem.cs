using Assets.SuperMouseRTS.Scripts.Players;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class PlayerStatusSystem : ComponentSystem
{
    private GameInfoController infoController = null;

    protected override void OnCreate()
    {
        base.OnCreate();

        if (!GameManager.Instance.IsSettingsLoaded)
        {
            GameManager.Instance.OnSettingsLoaded += OnSettingsLoaded;
            Enabled = false;
        }
        else
        {
            InitializeSystem();
        }

        infoController = GameObject.FindObjectOfType<GameInfoController>();
    }

    private void InitializeSystem()
    {
        Settings settings = GameManager.Instance.LoadedSettings;
        for (int i = 0; i < settings.Players; i++)
        {
            int id = i + 1;
            Entity ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData<PlayerID>(ent, new PlayerID(id));
            EntityManager.AddComponentData<Player>(ent, new Player());
            EntityManager.AddComponentData(ent, new PlayerMouseInfoDelay(0.1f));
            if(i >= settings.HumanPlayers)
            {
                Debug.Log("Adding ai tag to player: " + id);
                EntityManager.AddComponentData(ent, new AIPlayer(AIType.BloodThirsty));
            }

            infoController.AddPlayerInfo(id);
        }

        Enabled = true;

    }

    private void OnSettingsLoaded(Settings obj)
    {
        InitializeSystem();
    }




    protected override void OnUpdate()
    {
        var counts = new int[GameManager.Instance.LoadedSettings.Players];

        Entities.ForEach((ref PlayerID id, ref MovementSpeed speed) =>
        {
            counts[id.Value - 1]++;
        });

        for (int i = 0; i < counts.Length; i++)
        {
            infoController.SetPlayerInfo(i +1, counts[i].ToString());
        }
        //Entities.ForEach((ref PlayerID id, ref OreResources resources, ref SpawnScheduler timer) =>
        //{
        //    if(timer.TimeLeftToSpawn <= 0 && resources.Value >= GameManager.Instance.LoadedSettings.UnitCost)
        //    {
        //        resources.Value -= GameManager.Instance.LoadedSettings.UnitCost;
        
        //        timer.TimeLeftToSpawn = GameManager.Instance.LoadedSettings.UnitSpawnTime;
        //        timer.SpawnsOrdered++;
        //    }
        //});

    }

}