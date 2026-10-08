using System;
using _Project.Core.Localization;
using _Project.Core.Purchasing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Shop
{
    public class NoAdsShopView : MonoBehaviour
    {
        private const string OwnedKey = "shop.owned";

        [SerializeField] private Button _buyButton;
        [SerializeField] private TextMeshProUGUI _priceLabel;

        private IPurchaseService _purchases;
        private ILocalizationService _localization;
        private bool _started;

        [Inject]
        public void Construct(IPurchaseService purchases, ILocalizationService localization)
        {
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Start()
        {
            _started = true;
            _purchases.Changed += Refresh;

            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (_started)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_purchases != null)
            {
                _purchases.Changed -= Refresh;
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }
        }

        private void Refresh()
        {
            bool owned = _purchases.AdsDisabled;

            if (_buyButton != null)
            {
                _buyButton.interactable = owned == false;
            }

            if (_priceLabel != null)
            {
                _priceLabel.text = owned ? _localization.Get(OwnedKey) : _purchases.RemoveAdsPrice;
            }
        }

        private void OnBuyClicked()
        {
            _purchases.BuyRemoveAds();
        }
    }
}
