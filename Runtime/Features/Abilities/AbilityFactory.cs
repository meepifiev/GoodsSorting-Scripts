using System;
using _Project.Core.Abilities;

namespace _Project.Features.Abilities
{
    public class AbilityFactory : IAbilityFactory
    {
        private readonly IBoosterInventory _inventory;

        public AbilityFactory(IBoosterInventory inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public IAbility Create(AbilityConfig config, bool isUnlocked)
        {
            _inventory.EnsureSeeded(config.Type, config.InitialCount);
            return new Ability(config, isUnlocked, _inventory);
        }
    }
}
