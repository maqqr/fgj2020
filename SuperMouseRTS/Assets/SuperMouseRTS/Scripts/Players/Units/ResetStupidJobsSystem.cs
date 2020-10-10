using Assets.SuperMouseRTS.Scripts.GameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace Assets.SuperMouseRTS.Scripts.Players.Units
{
    public class ResetStupidJobsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            WorldStatusSystem sys = World.GetOrCreateSystem<WorldStatusSystem>();
            if (!sys.IsTileCacheReady)
            {
                return;
            }
            var tileCache = sys.TileCache;
            Entities.ForEach((ref UnitTarget target) =>
            {
                var tile = WorldStatusSystem.FromCache(tileCache, target.Value.Value.x, target.Value.Value.y, sys.TilesHorizontally);
                switch (target.Operation)
                {
                    case AIOperation.Repair:
                        
                        if (tile.tile != TileContent.Ruins)
                        {
                            target.Operation = AIOperation.Unassigned;
                            target.Priority = Priorities.NotSet;
                        }
                        break;
                    case AIOperation.Attack:
                        if (tile.tile == TileContent.Ruins)
                        {
                            target.Operation = AIOperation.Unassigned;
                            target.Priority = Priorities.NotSet;
                        }
                        break;
                    default:
                        break;
                }
            }).WithoutBurst().Run();
        }
    }
}
