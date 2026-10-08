using System;

namespace _Project.Core.Abilities
{
    public interface IAbility
    {
        AbilityType AbilityType { get; }
        int Count { get; }
        bool IsFree { get; }
        bool IsUnlocked { get; }
        int UnlockLevel { get; }
        event Action Changed;
        void Use();
        void Add(int amount);
    }
}
