using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Assets.SuperMouseRTS.Scripts.Players
{
    public struct AIPlayer : IComponentData
    {
        public AIType Type;

        public AIPlayer(AIType type)
        {
            Type = type;
        }
    }

    public enum AIType
    {
        BloodThirsty
    }
}
