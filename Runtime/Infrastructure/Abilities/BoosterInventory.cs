using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Abilities
{
    public class BoosterInventory : IBoosterInventory
    {
        private const string CountKeyPrefix = "booster_count_";
        private const string SeedKeyPrefix = "booster_seeded_";

        private readonly Dictionary<AbilityType, int> _counts = new Dictionary<AbilityType, int>();
        private readonly HashSet<AbilityType> _loaded = new HashSet<AbilityType>();

        public event Action<AbilityType> Changed;

        public int GetCount(AbilityType abilityType)
        {
            EnsureLoaded(abilityType);
            return _counts[abilityType];
        }

        public void Add(AbilityType abilityType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsureLoaded(abilityType);
            _counts[abilityType] += amount;
            Persist(abilityType);
            Changed?.Invoke(abilityType);
        }

        public bool TrySpend(AbilityType abilityType)
        {
            EnsureLoaded(abilityType);

            if (_counts[abilityType] <= 0)
            {
                return false;
            }

            _counts[abilityType] -= 1;
            Persist(abilityType);
            Changed?.Invoke(abilityType);
            return true;
        }

        public void EnsureSeeded(AbilityType abilityType, int initialCount)
        {
            EnsureLoaded(abilityType);

            if (ReadSeeded(abilityType))
            {
                return;
            }

            if (initialCount > 0)
            {
                _counts[abilityType] += initialCount;
                Persist(abilityType);
            }

            WriteSeeded(abilityType);

            if (initialCount > 0)
            {
                Changed?.Invoke(abilityType);
            }
        }

        private void EnsureLoaded(AbilityType abilityType)
        {
            if (_loaded.Contains(abilityType))
            {
                return;
            }

            _loaded.Add(abilityType);
            _counts[abilityType] = ReadCount(abilityType);
        }

        private int ReadCount(AbilityType abilityType)
        {
            try
            {
                return PrimeSDK.Data.GetInt(CountKeyPrefix + (int)abilityType, 0);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private bool ReadSeeded(AbilityType abilityType)
        {
            try
            {
                return PrimeSDK.Data.GetBool(SeedKeyPrefix + (int)abilityType, false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void WriteSeeded(AbilityType abilityType)
        {
            try
            {
                PrimeSDK.Data.SetBool(SeedKeyPrefix + (int)abilityType, true, true);
                PrimeSDK.Data.Save();
            }
            catch (Exception)
            {
            }
        }

        private void Persist(AbilityType abilityType)
        {
            try
            {
                PrimeSDK.Data.SetInt(CountKeyPrefix + (int)abilityType, _counts[abilityType], important: false);
                PrimeSDK.Data.Save();
            }
            catch (Exception)
            {
            }
        }
    }
}
