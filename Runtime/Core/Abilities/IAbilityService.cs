using System;

namespace _Project.Core.Abilities
{
    public interface IAbilityService
    {
        event Action<AbilityType> Used;

        bool TryGetAbility(AbilityType abilityType, out IAbility ability);
        IAbility GetAbility(AbilityType abilityType);
        bool TryUse(AbilityType abilityType);
    }
}
