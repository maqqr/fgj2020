using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Assets.SuperMouseRTS.Scripts.Players.AIPlayerControl
{
    public class AIDecisionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var unitCost = GameManager.Instance.LoadedSettings.UnitCost;
            var aiPlayers = new NativeList<int>(Allocator.TempJob);
            Entities.ForEach((in PlayerID id, in AIPlayer ai) =>
            {
                aiPlayers.Add(id.Value);
            }).WithBurst().Schedule();

            Entities.ForEach((ref SpawnScheduler scheduler, ref OreResources resources, in PlayerID id) =>
            {
                if (aiPlayers.Contains(id.Value) && resources.Value >= unitCost)
                {
                    resources.Value -= unitCost;
                    scheduler.SpawnsOrdered++;
                }
                
            }).WithBurst(Unity.Burst.FloatMode.Fast).Schedule();

            this.CompleteDependency();
            aiPlayers.Dispose();
        }
    }
}
