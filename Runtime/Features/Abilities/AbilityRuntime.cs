using _Project.Core.Abilities;

namespace _Project.Features.Abilities
{
    public class AbilityRuntime : IAbilityRuntime
    {
        public bool IsBusy { get; private set; }

        public void SetBusy(bool busy)
        {
            IsBusy = busy;
        }
    }
}
