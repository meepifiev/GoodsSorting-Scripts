using _Project.Core.Abilities;

namespace _Project.Features.Abilities
{
    public interface IAbilityFactory
    {
        IAbility Create(AbilityConfig config, bool isUnlocked);
    }
}
