using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Core.Localization;
using VContainer.Unity;

namespace _Project.UI.Gameplay.Abilities
{
    public class AbilityPanelPresenter : IInitializable, IDisposable
    {
        private readonly IAbilityService _abilityService;
        private readonly AbilityPanel _abilityPanel;
        private readonly BoosterGetMorePopup _getMorePopup;
        private readonly IBoosterInventory _boosterInventory;
        private readonly ILocalizationService _localization;
        private readonly List<AbilityPresenter> _abilityPresenters;
        private readonly List<Binding> _bindings;

        public AbilityPanelPresenter(
            IAbilityService abilityService,
            AbilityPanel abilityPanel,
            BoosterGetMorePopup getMorePopup,
            IBoosterInventory boosterInventory,
            ILocalizationService localization)
        {
            _abilityService = abilityService ?? throw new ArgumentNullException(nameof(abilityService));
            _abilityPanel = abilityPanel ?? throw new ArgumentNullException(nameof(abilityPanel));
            _getMorePopup = getMorePopup ?? throw new ArgumentNullException(nameof(getMorePopup));
            _boosterInventory = boosterInventory ?? throw new ArgumentNullException(nameof(boosterInventory));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _abilityPresenters = new List<AbilityPresenter>();
            _bindings = new List<Binding>();
        }

        public void Initialize()
        {
            foreach (AbilityView abilityView in _abilityPanel.AbilityViews)
            {
                if (_abilityService.TryGetAbility(abilityView.AbilityType, out IAbility ability) == false)
                {
                    continue;
                }

                _abilityPresenters.Add(new AbilityPresenter(ability, abilityView, _boosterInventory, _localization));

                AbilityType abilityType = abilityView.AbilityType;
                IAbility capturedAbility = ability;
                Action handler = () => OnAbilityClicked(capturedAbility, abilityType);
                abilityView.Clicked += handler;
                _bindings.Add(new Binding(abilityView, handler));
            }
        }

        public void Dispose()
        {
            foreach (Binding binding in _bindings)
            {
                binding.View.Clicked -= binding.Handler;
            }

            _bindings.Clear();

            foreach (AbilityPresenter abilityPresenter in _abilityPresenters)
            {
                abilityPresenter.Dispose();
            }
        }

        private void OnAbilityClicked(IAbility ability, AbilityType abilityType)
        {
            if (ability.IsUnlocked == false)
            {
                return;
            }

            if (ability.Count > 0)
            {
                _abilityService.TryUse(abilityType);
                return;
            }

            _getMorePopup.Open(abilityType);
        }

        private readonly struct Binding
        {
            public readonly AbilityView View;
            public readonly Action Handler;

            public Binding(AbilityView view, Action handler)
            {
                View = view;
                Handler = handler;
            }
        }
    }
}
