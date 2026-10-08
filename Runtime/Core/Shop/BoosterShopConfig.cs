using System.Collections.Generic;
using _Project.Core.Abilities;
using UnityEngine;

namespace _Project.Core.Shop
{
    [CreateAssetMenu(fileName = "BoosterShopConfig", menuName = "Configs/Booster Shop Config")]
    public class BoosterShopConfig : ScriptableObject
    {
        [SerializeField] private List<BoosterShopEntry> _entries = new List<BoosterShopEntry>();

        public IReadOnlyList<BoosterShopEntry> Entries => _entries;

        public BoosterShopEntry Find(AbilityType abilityType)
        {
            foreach (BoosterShopEntry entry in _entries)
            {
                if (entry.AbilityType == abilityType)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
