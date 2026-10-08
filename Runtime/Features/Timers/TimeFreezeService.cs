using System;
using System.Threading;
using _Project.Core.Time;
using Cysharp.Threading.Tasks;

namespace _Project.Features.Timers
{
    public class TimeFreezeService : ITimeFreeze, IDisposable
    {
        private readonly ILevelTimer _levelTimer;
        private CancellationTokenSource _cancellation;

        public TimeFreezeService(ILevelTimer levelTimer)
        {
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
        }

        public bool IsFrozen { get; private set; }

        public event Action<bool> FrozenChanged;

        public void Freeze(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();

            RunAsync(seconds, _cancellation.Token).Forget();
        }

        public void Dispose()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private async UniTaskVoid RunAsync(float seconds, CancellationToken cancellationToken)
        {
            if (IsFrozen == false)
            {
                IsFrozen = true;
                _levelTimer.Pause();
                FrozenChanged?.Invoke(true);
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            IsFrozen = false;
            _levelTimer.Resume();
            FrozenChanged?.Invoke(false);
        }
    }
}
