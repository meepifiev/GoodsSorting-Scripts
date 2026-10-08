using System;
using System.Collections.Generic;
using _Project.Core.Time;
using _Project.Features.CellSpawner;
using VContainer.Unity;

namespace _Project.Features.Services.InactivityHintServices
{
    public class InactivityHintService : IInactivityHintService, IInitializable, IDisposable, ITickable
    {
        private const float InactivityDelaySeconds = 10f;
        private const float HintDurationSeconds = 5f;

        private readonly List<ShelfItemView> _matchableItems = new();
        private readonly List<ShelfItemView> _hintedItems = new();
        private readonly List<ShelfItemView> _targetShelfItems = new();
        private readonly List<ShelfItemView> _sourceShelfItems = new();
        private readonly ILevelTimer _levelTimer;
        private readonly ITimeService _timeService;
        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly IAbilityHintService _abilityHintService;

        private float _inactivitySeconds;
        private float _hintSeconds;
        private bool _isHintShowing;
        private bool _isAbilityHintShowing;

        public InactivityHintService(
            ILevelTimer levelTimer,
            ITimeService timeService,
            LevelShelfSpawner levelShelfSpawner,
            IAbilityHintService abilityHintService)
        {
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _abilityHintService = abilityHintService ?? throw new ArgumentNullException(nameof(abilityHintService));
        }

        public void Initialize()
        {
            _levelShelfSpawner.LevelSpawned += OnLevelSpawned;
            _levelShelfSpawner.LevelCleared += OnLevelCleared;
        }

        public void Dispose()
        {
            _levelShelfSpawner.LevelSpawned -= OnLevelSpawned;
            _levelShelfSpawner.LevelCleared -= OnLevelCleared;
            StopHint();
        }

        public void RegisterPlayerActivity()
        {
            StopHint();
            ResetInactivityTimer();
        }

        public void Tick()
        {
            if (_levelTimer.IsRunning == false || _levelShelfSpawner.HasSpawnedLevel == false)
            {
                StopHint();
                return;
            }

            if (_isHintShowing)
            {
                _hintSeconds += _timeService.DeltaTime;

                if (_hintSeconds >= HintDurationSeconds)
                {
                    StopHint();
                    ResetInactivityTimer();
                }

                return;
            }

            _inactivitySeconds += _timeService.DeltaTime;

            if (_inactivitySeconds < InactivityDelaySeconds)
            {
                return;
            }

            ResetInactivityTimer();
            StartHint();
        }

        private void StartHint()
        {
            if (TryGetHintableItems())
            {
                foreach (ShelfItemView item in _matchableItems)
                {
                    item.RequestInactivityHint();
                    _hintedItems.Add(item);
                }

                _isHintShowing = true;
                return;
            }

            _abilityHintService.StartInactivityHint();
            _isHintShowing = true;
            _isAbilityHintShowing = true;
        }

        private bool TryGetHintableItems()
        {
            foreach (ShelfCellView cell in _levelShelfSpawner.SpawnedCells)
            {
                if (cell != null && cell.TryGetMatchableFrontLayerItems(_matchableItems))
                {
                    return true;
                }
            }

            foreach (ShelfCellView targetCell in _levelShelfSpawner.SpawnedCells)
            {
                if (targetCell == null || targetCell.HasFreeFrontSlot() == false ||
                    targetCell.TryGetFrontLayerItems(_targetShelfItems) == false)
                {
                    continue;
                }

                for (int targetItemIndex = 0; targetItemIndex < _targetShelfItems.Count; targetItemIndex++)
                {
                    ShelfItemView targetItem = _targetShelfItems[targetItemIndex];

                    if (targetItem.CanDrag == false)
                    {
                        continue;
                    }

                    _matchableItems.Clear();
                    _matchableItems.Add(targetItem);

                    for (int nextTargetItemIndex = targetItemIndex + 1;
                        nextTargetItemIndex < _targetShelfItems.Count;
                        nextTargetItemIndex++)
                    {
                        ShelfItemView nextTargetItem = _targetShelfItems[nextTargetItemIndex];

                        if (nextTargetItem.CanDrag == false || nextTargetItem.ItemId != targetItem.ItemId)
                        {
                            continue;
                        }

                        _matchableItems.Add(nextTargetItem);
                        break;
                    }

                    if (_matchableItems.Count < 2)
                    {
                        continue;
                    }

                    foreach (ShelfCellView sourceCell in _levelShelfSpawner.SpawnedCells)
                    {
                        if (sourceCell == null || sourceCell == targetCell ||
                            sourceCell.TryGetFrontLayerItems(_sourceShelfItems) == false)
                        {
                            continue;
                        }

                        foreach (ShelfItemView sourceItem in _sourceShelfItems)
                        {
                            if (sourceItem.CanDrag == false || sourceItem.ItemId != targetItem.ItemId)
                            {
                                continue;
                            }

                            _matchableItems.Add(sourceItem);
                            return true;
                        }
                    }
                }
            }

            _matchableItems.Clear();
            return false;
        }

        private void StopHint()
        {
            if (_isHintShowing == false)
            {
                return;
            }

            foreach (ShelfItemView item in _hintedItems)
            {
                if (item != null)
                {
                    item.StopInactivityHint();
                }
            }

            _hintedItems.Clear();

            if (_isAbilityHintShowing)
            {
                _abilityHintService.StopInactivityHint();
            }

            _isHintShowing = false;
            _isAbilityHintShowing = false;
            _hintSeconds = 0f;
        }

        private void ResetInactivityTimer()
        {
            _inactivitySeconds = 0f;
        }

        private void OnLevelSpawned(int _)
        {
            StopHint();
            ResetInactivityTimer();
        }

        private void OnLevelCleared()
        {
            StopHint();
            ResetInactivityTimer();
        }
    }
}
