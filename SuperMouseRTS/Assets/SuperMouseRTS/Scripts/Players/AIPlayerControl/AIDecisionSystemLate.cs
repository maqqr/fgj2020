using Assets.SuperMouseRTS.Scripts.GameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.SuperMouseRTS.Scripts.Players.AIPlayerControl
{
    [UpdateAfter(typeof(AIDecisionSystem))]
    public class AIDecisionSystemLate : SystemBase
    {
        private struct ConquerOperation
        {
            public int Id;
            public Entity entity;
            public TilePosition Start;
            public UnitTarget Unit;
            public TilePosition Target;
        }

        protected override void OnUpdate()
        {
            var decisionSystem = World.GetOrCreateSystem<AIDecisionSystem>();
            var owneds = decisionSystem.OwnedCache;
            var aiPlayers = decisionSystem.AIPlayersCache;

            var conquers = new NativeList<ConquerOperation>(Allocator.TempJob);

            Entities.ForEach((Entity ent, ref UnitTarget unitTarget, in PlayerID id, in OwnerBuilding owner) =>
            {
                var index = AIDecisionSystem.IndexOfAIPlayer(aiPlayers, id.Value);
                if (index != -1 && owneds[owner.OwnerTile] > aiPlayers[index].UnitsToConquer)
                {
                    conquers.Add(new ConquerOperation
                    {
                        Id = id.Value,
                        entity = ent,
                        Start = owner.OwnerTile,
                        Unit = unitTarget,
                        Target = new TilePosition(new Unity.Mathematics.int2(100000, 100000))
                    });
                }
            }).Schedule();

            Entities.ForEach((Entity ent, in TilePosition pos, in Tile tile) =>
            {
                if (tile.tile == TileContent.Ruins)
                {
                    for (int i = 0; i < conquers.Length; i++)
                    {
                        var conquer = conquers[i];
                        if (math.distance(conquer.Start.Value, conquer.Target.Value) > math.distance(conquer.Start.Value, pos.Value))
                        {
                            conquer.Target = pos;
                            conquer.Unit.Value = pos;
                            conquer.Unit.Operation = AIOperation.Repair;
                            conquer.Unit.Priority = Priorities.Important;
                        }
                        conquers[i] = conquer;
                    }
                }
                if (tile.tile == TileContent.Building)
                {
                    var targetId = EntityManager.GetComponentData<PlayerID>(ent).Value;

                    for (int i = 0; i < conquers.Length; i++)
                    {
                        var conquer = conquers[i];
                        if (targetId == conquer.Id)
                        {
                            continue;
                        }

                        if (math.distance(conquer.Start.Value, conquer.Target.Value) > math.distance(conquer.Start.Value, pos.Value))
                        {
                            conquer.Target = pos;
                            conquer.Unit.Value = pos;
                            conquer.Unit.Operation = AIOperation.Attack;
                            conquer.Unit.Priority = Priorities.Important;
                        }
                        conquers[i] = conquer;
                    }
                }

            }).WithoutBurst().Run();

            CompleteDependency();
            foreach (var conquerOperation in conquers)
            {
                EntityManager.SetComponentData(conquerOperation.entity, conquerOperation.Unit);
            }

            aiPlayers.Dispose();
            owneds.Dispose();
            conquers.Dispose();
        }
    }
}
