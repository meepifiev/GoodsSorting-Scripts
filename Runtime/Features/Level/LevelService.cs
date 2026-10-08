using System;
using _Project.Core.Building;
using _Project.Core.Level;
using _Project.Core.Progress;
using _Project.Core.StateMachine;
using _Project.Core.Time;
using _Project.Features.CellSpawner;
using VContainer.Unity;

namespace _Project.Features.Level
{
    public class LevelService : ILevelService, IInitializable, IDisposable
    {
        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly ILevelTimer _levelTimer;
        private readonly IProgressStorage _progressStorage;
        private readonly IGameLauncher _gameLauncher;
        private readonly IBuildingAccess _buildingAccess;
        private readonly ILevelCatalog _levelCatalog;

        private const float SpaceReviveFallbackSeconds = 30f;
        private const int SpaceReviveItemCount = 3;

        private int _remainingItems;
        private int _playedLevel;

        public LevelService(
            LevelShelfSpawner levelShelfSpawner,
            ILevelTimer levelTimer,
            IProgressStorage progressStorage,
            IGameLauncher gameLauncher,
            IBuildingAccess buildingAccess,
            ILevelCatalog levelCatalog)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
            _progressStorage = progressStorage ?? throw new ArgumentNullException(nameof(progressStorage));
            _gameLauncher = gameLauncher ?? throw new ArgumentNullException(nameof(gameLauncher));
            _buildingAccess = buildingAccess ?? throw new ArgumentNullException(nameof(buildingAccess));
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
        }

        public int CurrentLevel => _playedLevel;
        public LevelState State { get; private set; } = LevelState.Playing;
        public LevelLostReason LoseReason { get; private set; } = LevelLostReason.TimeUp;

        public event Action<LevelFinishResult> Finished;
        public event Action Resumed;

        public void Initialize()
        {
            _playedLevel = SkipDisabled(_progressStorage.Level);

            _levelShelfSpawner.LevelSpawned += OnLevelSpawned;
            _levelShelfSpawner.ItemsRemoved += OnItemsRemoved;
            _levelShelfSpawner.Deadlocked += OnDeadlocked;
            _levelTimer.TimeExpired += Lose;

            if (_levelShelfSpawner.HasSpawnedLevel)
            {
                OnLevelSpawned(_levelShelfSpawner.GetCurrentItemCount());
            }
        }

        public void Dispose()
        {
            _levelShelfSpawner.LevelSpawned -= OnLevelSpawned;
            _levelShelfSpawner.ItemsRemoved -= OnItemsRemoved;
            _levelShelfSpawner.Deadlocked -= OnDeadlocked;
            _levelTimer.TimeExpired -= Lose;
        }

        public void Win()
        {
            Finish(LevelFinishResult.Won);
        }

        public void Lose()
        {
            Lose(LevelLostReason.TimeUp);
        }

        public void Lose(LevelLostReason reason)
        {
            LoseReason = reason;
            Finish(LevelFinishResult.Lost);
        }

        private void OnDeadlocked()
        {
            if (State != LevelState.Playing || _remainingItems <= 0)
            {
                return;
            }

            LoseReason = LevelLostReason.OutOfSpace;
            Finish(LevelFinishResult.Lost);
        }

        public void Revive(float extraSeconds)
        {
            if (State != LevelState.Lost || extraSeconds <= 0f)
            {
                return;
            }

            State = LevelState.Playing;
            _levelTimer.StartCountdown(extraSeconds);
            Resumed?.Invoke();
        }

        public void ReviveWithSpace()
        {
            if (State != LevelState.Lost)
            {
                return;
            }

            State = LevelState.Playing;

            float remaining = _levelTimer.RemainingSeconds;
            _levelTimer.StartCountdown(remaining > 1f ? remaining : SpaceReviveFallbackSeconds);
            _levelShelfSpawner.FreeSpace(SpaceReviveItemCount);
            Resumed?.Invoke();
        }

        public void GoNext()
        {
            if (_buildingAccess.ShouldVisitAfterLevel(_playedLevel))
            {
                _gameLauncher.GoToBuilding();
                return;
            }

            _gameLauncher.StartGame();
        }

        public void Restart()
        {
            _gameLauncher.StartGame();
        }

        private void Finish(LevelFinishResult result)
        {
            if (State != LevelState.Playing)
            {
                return;
            }

            State = result == LevelFinishResult.Won ? LevelState.Won : LevelState.Lost;
            _levelTimer.Halt();

            if (result == LevelFinishResult.Won)
            {
                AdvanceProgress();
            }

            Finished?.Invoke(result);
        }

        private int SkipDisabled(int levelNumber)
        {
            if (_levelCatalog.IsPlayable(levelNumber))
            {
                return levelNumber;
            }

            int playable = _levelCatalog.FindPlayable(levelNumber, 1);

            if (playable == 0)
            {
                playable = _levelCatalog.FindPlayable(levelNumber, -1);
            }

            if (playable == 0 || playable == levelNumber)
            {
                return levelNumber;
            }

            _progressStorage.Level = playable;
            _progressStorage.Save();
            return playable;
        }

        private void AdvanceProgress()
        {
            int next = _levelCatalog.FindPlayable(_progressStorage.Level + 1, 1);

            if (next == 0)
            {
                return;
            }

            _progressStorage.Level = next;
            _progressStorage.Save();
        }

        private void OnLevelSpawned(int itemCount)
        {
            State = LevelState.Playing;
            _remainingItems = itemCount;

            if (_remainingItems == 0)
            {
                Win();
            }
        }

        private void OnItemsRemoved(int itemCount)
        {
            if (State != LevelState.Playing || itemCount <= 0)
            {
                return;
            }

            _remainingItems = Math.Max(0, _remainingItems - itemCount);

            if (_remainingItems == 0)
            {
                Win();
            }
        }
    }
}
