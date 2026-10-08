using System;
using _Project.Core.Abilities;
using _Project.Core.Time;

namespace _Project.Features.Abilities.Effects
{
    public class FreezeAbilityEffect : IAbilityEffect
    {
        private const float FreezeSeconds = 15f;

        private readonly ITimeFreeze _timeFreeze;

        public FreezeAbilityEffect(ITimeFreeze timeFreeze)
        {
            _timeFreeze = timeFreeze ?? throw new ArgumentNullException(nameof(timeFreeze));
        }

        public AbilityType Type => AbilityType.Freeze;

        public bool Apply()
        {
            _timeFreeze.Freeze(FreezeSeconds);
            return true;
        }
    }
}
