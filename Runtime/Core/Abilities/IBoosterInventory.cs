using System;

namespace _Project.Core.Abilities
{
    public interface IBoosterInventory
    {
        event Action<AbilityType> Changed;

        int GetCount(AbilityType abilityType);
        void Add(AbilityType abilityType, int amount);
        bool TrySpend(AbilityType abilityType);
        void EnsureSeeded(AbilityType abilityType, int initialCount);
    }
}
