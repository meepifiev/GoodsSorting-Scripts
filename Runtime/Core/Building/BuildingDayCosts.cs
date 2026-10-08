using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingDayCosts
    {
        private const int TypeCount = 5;

        [SerializeField] private int[] _costByType = new int[TypeCount];

        public BuildingDayCosts(int[] costByType)
        {
            if (costByType == null || costByType.Length != TypeCount)
            {
                throw new ArgumentException(nameof(costByType));
            }

            _costByType = costByType;
        }

        public int GetCost(BuildingTaskType type)
        {
            return _costByType[(int)type];
        }
    }
}
