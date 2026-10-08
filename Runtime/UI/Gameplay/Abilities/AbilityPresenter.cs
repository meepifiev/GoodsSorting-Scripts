using System;
using _Project.Core.Abilities;
using _Project.Core.Localization;

namespace _Project.UI.Gameplay.Abilities
{
    public class AbilityPresenter : IDisposable
    {
        private const string UnlockLevelKey = "ability.unlock_level";

        private readonly IAbility _ability;
        private readonly AbilityView _view;
        private readonly IBoosterInventory _inventory;
        private readonly ILocalizationService _localization;

        public AbilityPresenter(IAbility ability, AbilityView view, IBoosterInventory inventory, ILocalizationService localization)
        {
            _ability = ability ?? throw new ArgumentNullException(nameof(ability));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _ability.Changed += UpdateView;
            _inventory.Changed += OnInventoryChanged;
            UpdateView();
        }

        public void Dispose()
        {
            _ability.Changed -= UpdateView;
            _inventory.Changed -= OnInventoryChanged;
        }

        private void UpdateView()
        {
            string lockLabel = string.Format(_localization.Get(UnlockLevelKey), _ability.UnlockLevel);
            _view.SetLocked(_ability.IsUnlocked == false, lockLabel);

            if (_ability.IsUnlocked)
            {
                _view.SetState(_ability.Count, _ability.IsFree);
            }
        }

        private void OnInventoryChanged(AbilityType abilityType)
        {
            if (abilityType == _ability.AbilityType)
            {
                UpdateView();
            }
        }
    }
}
