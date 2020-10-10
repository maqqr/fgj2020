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
    public class ShowInfoWithMouseSystem : ComponentSystem
    {
        private RaycastSystem raycastSystem;
        private FactoryInformationPopupController factoryInfo;

        protected override void OnCreate()
        {
            raycastSystem = World.GetOrCreateSystem<RaycastSystem>();
            factoryInfo = GameObject.FindObjectOfType<FactoryInformationPopupController>();
        }

        protected override void OnUpdate()
        {
            //float deltaTime = UnityEngine.Time.deltaTime;
            //Entities.ForEach((ref PlayerMouseInfoDelay delay, in Player player, in PlayerID playerId) =>
            //{
            //    var pointerIndex = playerId.Value - 1;
            //    var pointer = MultiMouse.Instance.GetMouseByIndex(pointerIndex);
            //    if (pointer == null)
            //    {
            //        return;
            //    }
            //    var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
            //    var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };
            //    if (raycastSystem.Raycast(ray, out RaycastHit hit))
            //    {
            //        var isSame = hit.Entity == delay.EntityTargeted;
            //        if (!hit.Hit)
            //        {
            //            delay.DelayConsumed = delay.Delay;
            //        }

            //        if (isSame)
            //        {
            //            delay.DelayConsumed -= deltaTime;
            //        }
            //        else
            //        {
            //            delay.EntityTargeted = hit.Entity;
            //            delay.DelayConsumed = delay.Delay;
            //        }
            //        if(delay.DelayConsumed <= 0)
            //        {
            //            var translation = EntityManager.GetComponentData<Translation>(delay.EntityTargeted);
            //        }
            //    }               
                
            //});
            //factoryInfo.BroadcastMessage("");

        }
    }
}

