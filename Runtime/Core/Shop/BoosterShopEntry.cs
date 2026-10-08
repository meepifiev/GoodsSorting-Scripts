using System;
using _Project.Core.Abilities;
using UnityEngine;

namespace _Project.Core.Shop
{
    [Serializable]
    public class BoosterShopEntry
    {
        [SerializeField] private AbilityType _abilityType;
        [SerializeField] private int _coinPrice = 300;
        [SerializeField] private int _coinAmount = 1;
        [SerializeField] private int _adAmount = 1;

        public AbilityType AbilityType => _abilityType;
        public int CoinPrice => _coinPrice;
        public int CoinAmount => _coinAmount;
        public int AdAmount => _adAmount;
    }
}
