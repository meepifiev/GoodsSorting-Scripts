using System;
using _Project.Core.Combo;
using _Project.Core.Score;
using _Project.Features.CellSpawner;
using VContainer.Unity;

namespace _Project.Features.Score
{
    public class StarsCounter : IStarsCounter, IInitializable, IDisposable
    {
        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly IComboService _comboService;

        public StarsCounter(LevelShelfSpawner levelShelfSpawner, IComboService comboService)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _comboService = comboService ?? throw new ArgumentNullException(nameof(comboService));
        }

        public int Stars { get; private set; }

        public event Action<int> Changed;

        public void Initialize()
        {
            _comboService.Bumped += OnComboBumped;
            _levelShelfSpawner.LevelSpawned += OnLevelSpawned;
            _levelShelfSpawner.LevelCleared += Reset;
        }

        public void Dispose()
        {
            _comboService.Bumped -= OnComboBumped;
            _levelShelfSpawner.LevelSpawned -= OnLevelSpawned;
            _levelShelfSpawner.LevelCleared -= Reset;
        }

        private void OnComboBumped(int combo)
        {
            if (combo <= 0)
            {
                return;
            }

            Stars += combo;
            Changed?.Invoke(Stars);
        }

        private void OnLevelSpawned(int itemCount)
        {
            Reset();
        }

        private void Reset()
        {
            if (Stars == 0)
            {
                return;
            }

            Stars = 0;
            Changed?.Invoke(Stars);
        }
    }
}
