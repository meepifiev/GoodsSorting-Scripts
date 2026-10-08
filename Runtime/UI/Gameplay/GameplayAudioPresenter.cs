using System;
using _Project.Core.Abilities;
using _Project.Core.Audio;
using _Project.Core.Combo;
using _Project.Core.Level;
using _Project.Core.Time;
using _Project.Features.CellSpawner;
using UnityEngine;
using VContainer.Unity;

namespace _Project.UI.Gameplay
{
    public class GameplayAudioPresenter : IInitializable, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly GameplayAudioConfig _config;
        private readonly IComboService _comboService;
        private readonly IAbilityService _abilityService;
        private readonly ILevelTimer _levelTimer;
        private readonly ILevelService _levelService;
        private readonly ShelfItemDragController _dragController;
        private readonly StarFlyEffect _starFlyEffect;

        private int _lastCombo;

        public GameplayAudioPresenter(
            IAudioService audioService,
            GameplayAudioConfig config,
            IComboService comboService,
            IAbilityService abilityService,
            ILevelTimer levelTimer,
            ILevelService levelService,
            ShelfItemDragController dragController,
            StarFlyEffect starFlyEffect)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _comboService = comboService ?? throw new ArgumentNullException(nameof(comboService));
            _abilityService = abilityService ?? throw new ArgumentNullException(nameof(abilityService));
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _dragController = dragController ?? throw new ArgumentNullException(nameof(dragController));
            _starFlyEffect = starFlyEffect ?? throw new ArgumentNullException(nameof(starFlyEffect));
        }

        public void Initialize()
        {
            _comboService.MatchScored += OnMatchScored;
            _comboService.Ended += OnComboEnded;
            _abilityService.Used += OnAbilityUsed;
            _levelTimer.OneMinuteRemaining += OnTimerWarning;
            _levelService.Finished += OnLevelFinished;
            _dragController.ItemPicked += OnItemPicked;
            _dragController.ItemPlaced += OnItemPlaced;
            _starFlyEffect.StarReceived += OnStarReceived;
        }

        public void Dispose()
        {
            _comboService.MatchScored -= OnMatchScored;
            _comboService.Ended -= OnComboEnded;
            _abilityService.Used -= OnAbilityUsed;
            _levelTimer.OneMinuteRemaining -= OnTimerWarning;
            _levelService.Finished -= OnLevelFinished;
            _dragController.ItemPicked -= OnItemPicked;
            _dragController.ItemPlaced -= OnItemPlaced;
            _starFlyEffect.StarReceived -= OnStarReceived;
        }

        private void OnMatchScored(Vector3 worldPosition, int combo)
        {
            _lastCombo = combo;
            Play(_config.GetMatchSound(combo));
        }

        private void OnComboEnded()
        {
            if (_lastCombo >= 2)
            {
                Play(_config.ComboLost);
            }

            _lastCombo = 0;
        }

        private void OnAbilityUsed(AbilityType abilityType)
        {
            Play(_config.GetAbilitySound(abilityType));
        }

        private void OnTimerWarning()
        {
            Play(_config.TimerWarning);
        }

        private void OnLevelFinished(LevelFinishResult result)
        {
            if (result == LevelFinishResult.Won)
            {
                Play(_config.LevelWon);
            }
        }

        private void OnItemPicked()
        {
            Play(_config.ItemPickUp);
        }

        private void OnItemPlaced()
        {
            Play(_config.ItemPutDown);
        }

        private void OnStarReceived()
        {
            Play(_config.StarReceived);
        }

        private void Play(AudioAsset asset)
        {
            _audioService.PlayOneShotSafe(asset);
        }
    }
}
