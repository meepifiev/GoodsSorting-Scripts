using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Abilities
{
    [CreateAssetMenu(fileName = "AbilitiesConfig", menuName = "Configs/Abilities Config")]
    public class AbilitiesConfig : ScriptableObject
    {
        [SerializeField] private List<AbilityEntry> _abilities = new List<AbilityEntry>();

        public IReadOnlyList<AbilityEntry> Abilities => _abilities;

        [Serializable]
        public class AbilityEntry
        {
            [SerializeField] private AbilityType _type;
            [SerializeField] private int _initialCount;
            [SerializeField] private bool _freeWhenEmpty = true;
            [SerializeField] private int _unlockLevel = 1;

            public AbilityConfig ToConfig()
            {
                return new AbilityConfig(_type, _initialCount, _freeWhenEmpty, _unlockLevel);
            }
        }
    }
}
