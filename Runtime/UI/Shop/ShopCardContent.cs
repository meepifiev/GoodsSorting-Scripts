using System;
using System.Globalization;
using _Project.Core.Localization;
using _Project.Core.Shop;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Shop
{
    public class ShopCardContent : MonoBehaviour
    {
        [SerializeField] private string _productTag;
        [SerializeField] private TextMeshProUGUI _goldLabel;
        [SerializeField] private bool _groupThousands = true;
        [SerializeField] private TextMeshProUGUI[] _boosterCountLabels;
        [SerializeField] private TextMeshProUGUI _packTitleLabel;
        [SerializeField] private string _packTitleKey;

        private ShopConfig _config;
        private ILocalizationService _localization;

        [Inject]
        public void Construct(ShopConfig config, ILocalizationService localization)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Start()
        {
            Apply();
        }

        private void Apply()
        {
            ShopProductConfig product = _config.Find(_productTag);

            if (product == null)
            {
                return;
            }

            if (_goldLabel != null && product.Gold > 0)
            {
                _goldLabel.text = Format(product.Gold);
            }

            ApplyBoosterCounts(product);

            if (_packTitleLabel != null && string.IsNullOrEmpty(_packTitleKey) == false)
            {
                _packTitleLabel.text = _localization.Get(_packTitleKey) + " x" + FirstBoosterCount(product);
            }
        }

        private void ApplyBoosterCounts(ShopProductConfig product)
        {
            if (_boosterCountLabels == null)
            {
                return;
            }

            for (int i = 0; i < _boosterCountLabels.Length; i++)
            {
                if (_boosterCountLabels[i] == null)
                {
                    continue;
                }

                int count = i < product.Boosters.Count ? product.Boosters[i].Count : FirstBoosterCount(product);
                _boosterCountLabels[i].text = "x" + count;
            }
        }

        private int FirstBoosterCount(ShopProductConfig product)
        {
            return product.Boosters.Count > 0 ? product.Boosters[0].Count : 0;
        }

        private string Format(int value)
        {
            if (_groupThousands == false)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
        }
    }
}
