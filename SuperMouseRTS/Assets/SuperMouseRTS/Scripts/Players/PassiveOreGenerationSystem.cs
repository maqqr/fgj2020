using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Assets.SuperMouseRTS.Scripts.Players
{
    public class PassiveOreGenerationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = UnityEngine.Time.deltaTime;
            Entities.ForEach((ref OreResources oreResources, ref PassiveOreGeneration passiveGeneration) =>
            {
                passiveGeneration.Counter += deltaTime;
                if (passiveGeneration.Counter >= passiveGeneration.Interval)
                {
                    passiveGeneration.Counter -= passiveGeneration.Interval;
                    oreResources.Value += passiveGeneration.Value;
                }
            }).Schedule();
        }
    }
}
