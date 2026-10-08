using System;
using _Project.Core.Audio;
using _Project.Core.Purchasing;
using _Project.Core.Shop;
using UnityEngine;
using VContainer;

namespace _Project.UI.Shop
{
    public class ShopAudioPresenter : MonoBehaviour
    {
        [SerializeField] private AudioAsset _successSound;

        private IShopService _shop;
        private IPurchaseService _purchases;
        private IAudioService _audio;
        private bool _started;

        [Inject]
        public void Construct(IShopService shop, IPurchaseService purchases, IAudioService audio)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        private void Start()
        {
            _started = true;
            _shop.Purchased += OnPurchased;
            _purchases.Changed += OnRemoveAdsChanged;
        }

        private void OnDestroy()
        {
            if (_started == false)
            {
                return;
            }

            _shop.Purchased -= OnPurchased;
            _purchases.Changed -= OnRemoveAdsChanged;
        }

        private void PlaySuccess()
        {
            _audio.PlayOneShotSafe(_successSound);
        }

        private void OnPurchased(string productTag)
        {
            PlaySuccess();
        }

        private void OnRemoveAdsChanged()
        {
            PlaySuccess();
        }
    }
}
