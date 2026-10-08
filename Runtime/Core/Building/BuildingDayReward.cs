using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingDayReward
    {
        [SerializeField] private int _gold;
        [SerializeField] private int _hammer;
        [SerializeField] private int _swap;
        [SerializeField] private int _replace;
        [SerializeField] private int _freeze;

        public BuildingDayReward(int gold, int hammer, int swap, int replace, int freeze)
        {
            _gold = gold;
            _hammer = hammer;
            _swap = swap;
            _replace = replace;
            _freeze = freeze;
        }

        public int Gold => _gold;
        public int Hammer => _hammer;
        public int Swap => _swap;
        public int Replace => _replace;
        public int Freeze => _freeze;
    }
}
