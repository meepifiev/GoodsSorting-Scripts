namespace _Project.Core.Abilities
{
    public interface IAbilityEffect
    {
        AbilityType Type { get; }
        bool Apply();
    }
}
