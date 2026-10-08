using System;
using _Project.Core.Abilities;
using UnityEngine;

namespace _Project.Core.Shop
{
    [Serializable]
    public class BoosterReward
    {
        [SerializeField] private AbilityType _abilityType;
        [SerializeField] private int _count;

        public AbilityType AbilityType => _abilityType;
        public int Count => _count;
    }
}
