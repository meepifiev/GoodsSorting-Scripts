using System;
using _Project.Core.Time;
using VContainer.Unity;

namespace _Project.Infrastructure.Time
{
    public class LevelTimer : ILevelTimer, ITickable
    {
        private const int OneMinuteSeconds = 60;
        private const int LastSecondsThreshold = 15;

        private readonly ITimeService _timeService;

        public LevelTimer(ITimeService timeService)
        {
            _timeService = timeService;
        }
        
        private float _remainingSeconds;
        private bool _isHalted;
        public event Action<float> RemainingTimeChanged;
        public event Action OneMinuteRemaining;
        public event Action LastSecondsTicked;
        public event Action TimeExpired;

        public bool IsRunning { get; private set; }
        public float RemainingSeconds => _remainingSeconds;

        public void StartCountdown(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            _remainingSeconds = durationSeconds;
            _isHalted = false;
            IsRunning = true;
            OnRemainingTimeChanged();
            NotifyWarnings(GetDisplayedSeconds(), GetDisplayedSeconds());
        }

        public void AddTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            int previousDisplayedSeconds = GetDisplayedSeconds();
            _remainingSeconds += seconds;
            OnRemainingTimeChanged();
            NotifyWarnings(previousDisplayedSeconds, GetDisplayedSeconds());
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            if (_isHalted || _remainingSeconds <= 0f)
            {
                return;
            }

            IsRunning = true;
        }

        public void Halt()
        {
            IsRunning = false;
            _isHalted = true;
        }

        public void Stop()
        {
            if (_remainingSeconds <= 0f && IsRunning == false)
            {
                return;
            }

            _remainingSeconds = 0f;
            IsRunning = false;
            OnRemainingTimeChanged();
        }

        private int GetDisplayedSeconds()
        {
            return (int)Math.Ceiling(_remainingSeconds);
        }

        private void OnRemainingTimeChanged()
        {
            RemainingTimeChanged?.Invoke(_remainingSeconds);
        }

        private void NotifyWarnings(
            int previousDisplayedSeconds,
            int currentDisplayedSeconds)
        {
            if (previousDisplayedSeconds > OneMinuteSeconds && currentDisplayedSeconds <= OneMinuteSeconds ||
                previousDisplayedSeconds == OneMinuteSeconds && currentDisplayedSeconds == OneMinuteSeconds)
            {
                OneMinuteRemaining?.Invoke();
            }

            if (currentDisplayedSeconds <= LastSecondsThreshold && currentDisplayedSeconds > 0)
            {
                LastSecondsTicked?.Invoke();
            }
        }

        private void OnTimeExpired()
        {
            TimeExpired?.Invoke();
        }

        public void Tick()
        {
            if (IsRunning == false)
            {
                return;
            }

            int previousDisplayedSeconds = GetDisplayedSeconds();
            _remainingSeconds = Math.Max(0f, _remainingSeconds - _timeService.DeltaTime);

            int currentDisplayedSeconds = GetDisplayedSeconds();

            if (previousDisplayedSeconds != currentDisplayedSeconds)
            {
                OnRemainingTimeChanged();
                NotifyWarnings(previousDisplayedSeconds, currentDisplayedSeconds);
            }

            if (_remainingSeconds > 0f)
            {
                return;
            }

            IsRunning = false;
            OnTimeExpired();
        }
    }
}
