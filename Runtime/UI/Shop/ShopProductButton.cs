using System;
using _Project.Core.Localization;
using _Project.Core.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Shop
{
    public class ShopProductButton : MonoBehaviour
    {
        private const string OwnedKey = "shop.owned";

        [SerializeField] private string _productTag;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TextMeshProUGUI _priceLabel;
        [SerializeField] private bool _showPrice = true;

        private IShopService _shop;
        private ILocalizationService _localization;
        private bool _started;

        [Inject]
        public void Construct(IShopService shop, ILocalizationService localization)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Start()
        {
            _started = true;
            _shop.Changed += Refresh;

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
            if (_shop != null)
            {
                _shop.Changed -= Refresh;
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }
        }

        private void Refresh()
        {
            bool owned = _shop.IsOwned(_productTag);

            if (_buyButton != null)
            {
                _buyButton.interactable = owned == false;
            }

            if (_priceLabel == null || _showPrice == false)
            {
                return;
            }

            if (owned)
            {
                _priceLabel.text = _localization.Get(OwnedKey);
                return;
            }

            string price = _shop.GetPrice(_productTag);

            if (string.IsNullOrEmpty(price) == false)
            {
                _priceLabel.text = price;
            }
        }

        private void OnBuyClicked()
        {
            _shop.Purchase(_productTag);
        }
    }
}
