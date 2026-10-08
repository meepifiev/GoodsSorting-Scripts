using System;

namespace _Project.Core.Pause
{
    public interface IPauseService
    {
        bool IsPaused { get; }
        event Action<bool> Changed;
        void Toggle();
        void Pause();
        void Resume();
    }
}
