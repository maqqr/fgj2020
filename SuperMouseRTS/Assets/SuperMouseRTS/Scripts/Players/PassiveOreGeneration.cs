using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Assets.SuperMouseRTS.Scripts.Players
{
    public struct PassiveOreGeneration : IComponentData
    {
        public int Value;
        public float Counter;
        public float Interval;

        public PassiveOreGeneration(int value, float interval)
        {
            Value = value;
            Counter = 0;
            Interval = interval;
        }
    }
}
