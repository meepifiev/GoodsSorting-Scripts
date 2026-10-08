using System;
using _Project.Core.Advertising;
using _Project.Core.Audio;
using _Project.Core.Economy;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Shop
{
    public class RewardedCoinsButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private int _amount = 80;
        [SerializeField] private string _rewardId = "free_coins";
        [SerializeField] private AudioAsset _successSound;

        private IAdvertisingService _ads;
        private IWalletStorage _wallet;
        private IAudioService _audio;

        [Inject]
        public void Construct(IAdvertisingService ads, IWalletStorage wallet, IAudioService audio)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            _ads.ShowRewarded(_rewardId, Grant, null);
        }

        private void Grant()
        {
            _wallet.Add(ResourceType.Gold, _amount);

            _audio.PlayOneShotSafe(_successSound);
        }
    }
}
