using System;
using _Project.Core.Combo;
using _Project.Features.CellSpawner;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Features.Combo
{
    public class ComboService : IComboService, IInitializable, IDisposable, ITickable
    {
        private const float ComboWindowSeconds = 3f;

        private readonly LevelShelfSpawner _levelShelfSpawner;
        private float _timeLeft;

        public ComboService(LevelShelfSpawner levelShelfSpawner)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
        }

        public int Combo { get; private set; }
        public float Progress { get; private set; }

        public event Action<int> Bumped;
        public event Action ProgressChanged;
        public event Action Ended;
        public event Action<Vector3, int> MatchScored;

        public void Initialize()
        {
            _levelShelfSpawner.Matched += OnMatched;
            _levelShelfSpawner.LevelSpawned += OnLevelSpawned;
            _levelShelfSpawner.LevelCleared += Reset;
        }

        public void Dispose()
        {
            _levelShelfSpawner.Matched -= OnMatched;
            _levelShelfSpawner.LevelSpawned -= OnLevelSpawned;
            _levelShelfSpawner.LevelCleared -= Reset;
        }

        public void Tick()
        {
            if (Combo <= 0)
            {
                return;
            }

            _timeLeft -= Time.deltaTime;

            if (_timeLeft <= 0f)
            {
                Reset();
                return;
            }

            Progress = _timeLeft / ComboWindowSeconds;
            ProgressChanged?.Invoke();
        }

        private void OnMatched(Vector3 worldPosition, int itemCount)
        {
            Combo++;
            _timeLeft = ComboWindowSeconds;
            Progress = 1f;
            Bumped?.Invoke(Combo);
            ProgressChanged?.Invoke();
            MatchScored?.Invoke(worldPosition, Combo);
        }

        private void OnLevelSpawned(int itemCount)
        {
            Reset();
        }

        private void Reset()
        {
            if (Combo == 0 && Progress == 0f)
            {
                return;
            }

            Combo = 0;
            _timeLeft = 0f;
            Progress = 0f;
            Ended?.Invoke();
        }
    }
}
