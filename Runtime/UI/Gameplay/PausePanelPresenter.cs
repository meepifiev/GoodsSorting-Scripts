using System;
using _Project.Core.Audio;
using _Project.Core.Pause;
using _Project.Core.StateMachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class PausePanelPresenter : MonoBehaviour
    {
        [Header("Show / hide")]
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private float _showDuration = 0.28f;
        [SerializeField] private float _fromScale = 0.8f;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _soundButton;
        [SerializeField] private Button _musicButton;

        [Header("Toggle icons")]
        [SerializeField] private Image _soundIcon;
        [SerializeField] private Image _musicIcon;
        [SerializeField] private Sprite _soundOnSprite;
        [SerializeField] private Sprite _soundOffSprite;
        [SerializeField] private Sprite _musicOnSprite;
        [SerializeField] private Sprite _musicOffSprite;

        private IPauseService _pauseService;
        private IGameLauncher _launcher;
        private ISoundSettings _soundSettings;

        private Sequence _showSequence;

        [Inject]
        public void Construct(IPauseService pauseService, IGameLauncher launcher, ISoundSettings soundSettings)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _soundSettings = soundSettings ?? throw new ArgumentNullException(nameof(soundSettings));
        }

        private void OnEnable()
        {
            _pauseService.Changed += OnPauseChanged;
            _soundSettings.Changed += RefreshIcons;

            AddClick(_continueButton, OnContinue);
            AddClick(_quitButton, OnQuit);
            AddClick(_soundButton, OnToggleSound);
            AddClick(_musicButton, OnToggleMusic);

            RefreshIcons();
            ApplyState(_pauseService.IsPaused, animate: false);
        }

        private void OnDisable()
        {
            if (_pauseService != null)
            {
                _pauseService.Changed -= OnPauseChanged;
            }

            if (_soundSettings != null)
            {
                _soundSettings.Changed -= RefreshIcons;
            }

            RemoveClick(_continueButton, OnContinue);
            RemoveClick(_quitButton, OnQuit);
            RemoveClick(_soundButton, OnToggleSound);
            RemoveClick(_musicButton, OnToggleMusic);

            _showSequence?.Kill();
            _showSequence = null;
        }

        private void OnPauseChanged(bool paused)
        {
            ApplyState(paused, animate: true);
        }

        private void ApplyState(bool paused, bool animate)
        {
            _showSequence?.Kill();
            _showSequence = null;

            if (_root != null)
            {
                _root.SetActive(paused);
            }

            if (paused == false)
            {
                return;
            }

            if (animate == false || _panel == null)
            {
                SetShown();
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            _panel.localScale = Vector3.one * _fromScale;

            _showSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (_canvasGroup != null)
            {
                _showSequence.Join(
                    DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 1f, _showDuration)
                        .SetUpdate(true));
            }

            _showSequence.Join(
                _panel.DOScale(1f, _showDuration).SetEase(Ease.OutBack).SetUpdate(true));
        }

        private void SetShown()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_panel != null)
            {
                _panel.localScale = Vector3.one;
            }
        }

        private void OnContinue()
        {
            _pauseService.Resume();
        }

        private void OnQuit()
        {
            _pauseService.Resume();
            _launcher.GoToMenu();
        }

        private void OnToggleSound()
        {
            _soundSettings.ToggleSfx();
        }

        private void OnToggleMusic()
        {
            _soundSettings.ToggleMusic();
        }

        private void RefreshIcons()
        {
            if (_soundIcon != null)
            {
                _soundIcon.sprite = _soundSettings.SfxEnabled ? _soundOnSprite : _soundOffSprite;
            }

            if (_musicIcon != null)
            {
                _musicIcon.sprite = _soundSettings.MusicEnabled ? _musicOnSprite : _musicOffSprite;
            }
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
