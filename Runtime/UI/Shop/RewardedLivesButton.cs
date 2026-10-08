using System;
using _Project.Core.Advertising;
using _Project.Core.Audio;
using _Project.Core.Lives;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Shop
{
    public class RewardedLivesButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _rewardId = "free_lives";
        [SerializeField] private AudioAsset _successSound;

        private IAdvertisingService _ads;
        private ILivesService _lives;
        private IAudioService _audio;

        [Inject]
        public void Construct(IAdvertisingService ads, ILivesService lives, IAudioService audio)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));

            _lives.Changed += Refresh;
            Refresh();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (_lives != null)
            {
                _lives.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            if (_button == null || _lives == null)
            {
                return;
            }

            _button.interactable = _lives.IsInfinite == false && _lives.Current < _lives.Max;
        }

        private void OnClicked()
        {
            _ads.ShowRewarded(_rewardId, Grant, null);
        }

        private void Grant()
        {
            _lives.Fill();

            _audio.PlayOneShotSafe(_successSound);

            Refresh();
        }
    }
}
