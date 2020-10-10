using Assets.SuperMouseRTS.Scripts.GameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.SuperMouseRTS.Scripts.Players.AIPlayerControl
{
    public class AIDecisionSystem : SystemBase
    {
        public NativeHashMap<TilePosition, int> OwnedCache;
        public NativeList<AIPlayerDataTransport> AIPlayersCache;

        public struct AIPlayerDataTransport
        {
            public int Id;
            public AIType Type;
            public int UnitsToConquer;

            public AIPlayerDataTransport(int id, AIType type, int unitsToConquer)
            {
                Id = id;
                Type = type;
                UnitsToConquer = unitsToConquer;
            }
        }

        protected override void OnUpdate()
        {
            var unitCost = GameManager.Instance.LoadedSettings.UnitCost;
            var aiPlayers = new NativeList<AIPlayerDataTransport>(Allocator.Persistent);
            Entities.ForEach((in PlayerID id, in AIPlayer ai) =>
            {
                aiPlayers.Add(new AIPlayerDataTransport(id.Value, ai.Type, ai.UnitsToConquer));
            }).Run();

            Entities.ForEach((ref SpawnScheduler scheduler, ref OreResources resources, in PlayerID id) =>
            {
                if (IndexOfAIPlayer(aiPlayers, id.Value) != -1 && resources.Value >= unitCost)
                {
                    resources.Value -= unitCost;
                    scheduler.SpawnsOrdered++;
                }
                
            }).WithBurst().Schedule();

            var owneds = new NativeHashMap<TilePosition, int>(100, Allocator.Persistent);
            Entities.ForEach((in PlayerID id, in OwnerBuilding owner, in UnitTarget unitTarget) =>
            {
                if (IndexOfAIPlayer(aiPlayers, id.Value) != -1)
                {
                    var count = owneds.ContainsKey(owner.OwnerTile) ? owneds[owner.OwnerTile] : 0;
                    owneds[owner.OwnerTile] = count + 1;
                }
            }).WithBurst().Schedule();

            AIPlayersCache = aiPlayers;
            OwnedCache = owneds;
        }

        public static int IndexOfAIPlayer(NativeList<AIPlayerDataTransport> aiPlayers, int playerId)
        {
            for (int i = 0; i < aiPlayers.Length; i++)
            {
                if (aiPlayers[i].Id == playerId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
