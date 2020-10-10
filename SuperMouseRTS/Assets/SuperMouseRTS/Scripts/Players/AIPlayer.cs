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
        public int UnitsToConquer;

        public AIPlayer(AIType type, int unitsToConquer)
        {
            Type = type;
            UnitsToConquer = unitsToConquer;
        }
    }

    public enum AIType
    {
        BloodThirsty,
        Peaceful

    }
}
