using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Core.Progress;
using _Project.Core.Time;

namespace _Project.Features.Abilities
{
    public class AbilityService : IAbilityService
    {
        private readonly Dictionary<AbilityType, IAbility> _abilities;
        private readonly Dictionary<AbilityType, IAbilityEffect> _effects;
        private readonly IAbilityRuntime _abilityRuntime;
        private readonly ITimeFreeze _timeFreeze;

        public event Action<AbilityType> Used;

        public AbilityService(
            IAbilityFactory abilityFactory,
            IProgressStorage progressStorage,
            AbilitiesConfig config,
            IReadOnlyList<IAbilityEffect> effects,
            IAbilityRuntime abilityRuntime,
            ITimeFreeze timeFreeze)
        {
            _abilityRuntime = abilityRuntime ?? throw new ArgumentNullException(nameof(abilityRuntime));
            _timeFreeze = timeFreeze ?? throw new ArgumentNullException(nameof(timeFreeze));

            if (abilityFactory == null)
            {
                throw new ArgumentNullException(nameof(abilityFactory));
            }

            if (progressStorage == null)
            {
                throw new ArgumentNullException(nameof(progressStorage));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            int currentLevel = progressStorage.Level;
            _abilities = new Dictionary<AbilityType, IAbility>();

            foreach (AbilitiesConfig.AbilityEntry entry in config.Abilities)
            {
                AbilityConfig abilityConfig = entry.ToConfig();
                bool isUnlocked = currentLevel >= abilityConfig.UnlockLevel;
                _abilities[abilityConfig.Type] = abilityFactory.Create(abilityConfig, isUnlocked);
            }

            _effects = new Dictionary<AbilityType, IAbilityEffect>();

            if (effects != null)
            {
                foreach (IAbilityEffect effect in effects)
                {
                    _effects[effect.Type] = effect;
                }
            }
        }

        public bool TryUse(AbilityType abilityType)
        {
            if (_abilities.TryGetValue(abilityType, out IAbility ability) == false)
            {
                return false;
            }

            if (ability.IsUnlocked == false)
            {
                return false;
            }

            if (ability.Count <= 0 && ability.IsFree == false)
            {
                return false;
            }

            if (_abilityRuntime.IsBusy)
            {
                return false;
            }

            if (abilityType == AbilityType.Freeze && _timeFreeze.IsFrozen)
            {
                return false;
            }

            bool wasFree = ability.IsFree;
            ability.Use();

            if (_effects.TryGetValue(abilityType, out IAbilityEffect effect))
            {
                bool applied = effect.Apply();

                if (applied == false && wasFree == false)
                {
                    ability.Add(1);
                    return false;
                }
            }

            Used?.Invoke(abilityType);
            return true;
        }

        public bool TryGetAbility(AbilityType abilityType, out IAbility ability)
        {
            return _abilities.TryGetValue(abilityType, out ability);
        }

        public IAbility GetAbility(AbilityType abilityType)
        {
            if (_abilities.TryGetValue(abilityType, out IAbility ability) == false)
            {
                throw new KeyNotFoundException(nameof(abilityType));
            }

            return ability;
        }
    }
}
