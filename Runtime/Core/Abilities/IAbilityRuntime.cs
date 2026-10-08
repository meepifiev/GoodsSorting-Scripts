namespace _Project.Core.Abilities
{
    public interface IAbilityRuntime
    {
        bool IsBusy { get; }
        void SetBusy(bool busy);
    }
}
