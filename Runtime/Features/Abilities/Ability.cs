using System;
using _Project.Core.Abilities;

namespace _Project.Features.Abilities
{
    public class Ability : IAbility
    {
        private readonly IBoosterInventory _inventory;
        private readonly bool _freeWhenEmpty;

        public Ability(AbilityConfig config, bool isUnlocked, IBoosterInventory inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            AbilityType = config.Type;
            _freeWhenEmpty = config.FreeWhenEmpty;
            UnlockLevel = config.UnlockLevel;
            IsUnlocked = isUnlocked;
        }

        public AbilityType AbilityType { get; }
        public int Count => _inventory.GetCount(AbilityType);
        public bool IsFree => _inventory.GetCount(AbilityType) == 0 && _freeWhenEmpty;
        public bool IsUnlocked { get; }
        public int UnlockLevel { get; }

        public event Action Changed;

        public void Use()
        {
            if (IsUnlocked == false || _inventory.GetCount(AbilityType) <= 0)
            {
                return;
            }

            _inventory.TrySpend(AbilityType);
            Changed?.Invoke();
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _inventory.Add(AbilityType, amount);
            Changed?.Invoke();
        }
    }
}
