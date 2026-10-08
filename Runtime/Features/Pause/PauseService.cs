using System;
using _Project.Core.Pause;
using _Project.Core.Time;
using UnityEngine;

namespace _Project.Features.Pause
{
    public class PauseService : IPauseService
    {
        private const float RunningTimeScale = 1f;
        private const float PausedTimeScale = 0f;

        private readonly ILevelTimer _levelTimer;

        public PauseService(ILevelTimer levelTimer)
        {
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
        }

        public bool IsPaused { get; private set; }

        public event Action<bool> Changed;

        public void Toggle()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            _levelTimer.Pause();
            Time.timeScale = PausedTimeScale;
            Changed?.Invoke(true);
        }

        public void Resume()
        {
            if (IsPaused == false)
            {
                return;
            }

            IsPaused = false;
            _levelTimer.Resume();
            Time.timeScale = RunningTimeScale;
            Changed?.Invoke(false);
        }
    }
}
