using System;

namespace _Project.Core.Time
{
    public interface ILevelTimer
    {
        event Action<float> RemainingTimeChanged;
        event Action OneMinuteRemaining;
        event Action LastSecondsTicked;
        event Action TimeExpired;

        bool IsRunning { get; }
        float RemainingSeconds { get; }

        void StartCountdown(float durationSeconds);
        void AddTime(float seconds);
        void Pause();
        void Resume();
        void Stop();

        void Halt();
    }
}
