using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Assets.SuperMouseRTS.Scripts.Players
{
    public struct PlayerMouseInfoDelay : IComponentData
    {
        public float Delay;
        public float DelayConsumed;
        public Entity EntityTargeted;
        public bool IsShowing;

        public PlayerMouseInfoDelay(float delay)
        {
            Delay = delay;
            DelayConsumed = 0;
            EntityTargeted = new Entity();
            IsShowing = false;
        }
    }
}
