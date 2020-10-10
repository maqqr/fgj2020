using Assets.SuperMouseRTS.Scripts.Players;
using Assets.SuperMouseRTS.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.SuperMouseRTS.Scripts.GameWorld
{
    [UpdateAfter(typeof(RaycastSystem))]
    public class ShowInfoWithMouseSystem : SystemBase
    {
        private RaycastSystem raycastSystem;

        protected override void OnCreate()
        {
            raycastSystem = World.GetOrCreateSystem<RaycastSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = UnityEngine.Time.deltaTime;
            Entities.ForEach((ref PlayerMouseInfoDelay delay, ref Player player, in PlayerID playerId) =>
            {
                var pointerIndex = playerId.Value - 1;
                var pointer = MultiMouse.Instance.GetMouseByIndex(pointerIndex);
                if (pointer == null)
                {
                    return;
                }
                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
                var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };
                if (raycastSystem.Raycast(ray, out RaycastHit hit))
                {
                    HandleRay(ref delay, deltaTime, hit);
                }
                else
                {
                    delay.EntityTargeted = Entity.Null;
                    HideFactoryPopup(ref delay);
                }
            }).WithoutBurst().Run();


        }

        private void HandleRay(ref PlayerMouseInfoDelay delay, float deltaTime, RaycastHit hit)
        {
            var isSame = hit.Entity == delay.EntityTargeted;

            if (isSame)
            {
                delay.DelayConsumed -= deltaTime;
            }
            else
            {
                delay.EntityTargeted = hit.Entity;
                HideFactoryPopup(ref delay);
            }

            if (delay.DelayConsumed <= 0)
            {
                delay.IsShowing = true;
                var targetEntity = delay.EntityTargeted;
                if (EntityManager.HasComponent<OreResources>(targetEntity))
                {
                    var tilePosition = EntityManager.GetComponentData<TilePosition>(targetEntity);
                    var resources = EntityManager.GetComponentData<OreResources>(targetEntity);
                    var spawn = EntityManager.GetComponentData<SpawnScheduler>(targetEntity);
                    var pos = WorldCoordinateTools.WorldToUnityCoordinate(tilePosition.Value.x, tilePosition.Value.y);

                    InformationPopupController.ShowFactoryInformation(pos, resources, spawn);
                }
            }

        }

        private static void HideFactoryPopup(ref PlayerMouseInfoDelay delay)
        {
            delay.DelayConsumed = delay.Delay;
            delay.IsShowing = false;
            InformationPopupController.DisablePopup();
        }
    }
}

