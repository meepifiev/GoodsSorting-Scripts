using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Shop
{
    [Serializable]
    public class ShopProductConfig
    {
        [SerializeField] private string _productTag;
        [SerializeField] private bool _isConsumable = true;
        [SerializeField] private int _gold;
        [SerializeField] private int _lives;
        [SerializeField] private bool _grantInfiniteLives;
        [SerializeField] private List<BoosterReward> _boosters = new List<BoosterReward>();

        public string ProductTag => _productTag;
        public bool IsConsumable => _isConsumable;
        public int Gold => _gold;
        public int Lives => _lives;
        public bool GrantInfiniteLives => _grantInfiniteLives;
        public IReadOnlyList<BoosterReward> Boosters => _boosters;
    }
}
