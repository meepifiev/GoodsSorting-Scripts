using System;

namespace _Project.Core.Time
{
    public interface ITimeFreeze
    {
        bool IsFrozen { get; }
        event Action<bool> FrozenChanged;
        void Freeze(float seconds);
    }
}
