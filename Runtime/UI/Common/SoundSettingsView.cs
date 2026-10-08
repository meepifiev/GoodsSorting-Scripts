using System;
using _Project.Core.Audio;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Common
{
    public class SoundSettingsView : MonoBehaviour
    {
        [SerializeField] private Button _soundButton;
        [SerializeField] private Button _musicButton;
        [SerializeField] private Image _soundIcon;
        [SerializeField] private Image _musicIcon;
        [SerializeField] private Sprite _soundOnSprite;
        [SerializeField] private Sprite _soundOffSprite;
        [SerializeField] private Sprite _musicOnSprite;
        [SerializeField] private Sprite _musicOffSprite;

        private ISoundSettings _settings;

        [Inject]
        public void Construct(ISoundSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        private void OnEnable()
        {
            if (_soundButton != null)
            {
                _soundButton.onClick.AddListener(OnToggleSound);
            }

            if (_musicButton != null)
            {
                _musicButton.onClick.AddListener(OnToggleMusic);
            }

            if (_settings != null)
            {
                _settings.Changed += RefreshIcons;
            }

            RefreshIcons();
        }

        private void OnDisable()
        {
            if (_soundButton != null)
            {
                _soundButton.onClick.RemoveListener(OnToggleSound);
            }

            if (_musicButton != null)
            {
                _musicButton.onClick.RemoveListener(OnToggleMusic);
            }

            if (_settings != null)
            {
                _settings.Changed -= RefreshIcons;
            }
        }

        private void OnToggleSound()
        {
            _settings.ToggleSfx();
        }

        private void OnToggleMusic()
        {
            _settings.ToggleMusic();
        }

        private void RefreshIcons()
        {
            if (_settings == null)
            {
                return;
            }

            if (_soundIcon != null)
            {
                _soundIcon.sprite = _settings.SfxEnabled ? _soundOnSprite : _soundOffSprite;
            }

            if (_musicIcon != null)
            {
                _musicIcon.sprite = _settings.MusicEnabled ? _musicOnSprite : _musicOffSprite;
            }
        }
    }
}
