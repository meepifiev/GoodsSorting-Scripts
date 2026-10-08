using System;
using _Project.Core.Abilities;
using _Project.Core.Audio;
using _Project.Core.Shop;
using _Project.Core.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay.Abilities
{
    public class BoosterGetMorePopup : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _boosterIcon;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _coinButton;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private AudioAsset _successSound;
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _fromScale = 0.8f;

        [SerializeField] private Sprite _freezeIcon;
        [SerializeField] private Sprite _hammerIcon;
        [SerializeField] private Sprite _swapIcon;
        [SerializeField] private Sprite _replaceIcon;

        private IBoosterShop _shop;
        private IAudioService _audio;
        private IModalGate _modalGate;
        private AbilityType _current;
        private Tween _showTween;
        private bool _gateOpen;

        [Inject]
        public void Construct(IBoosterShop shop, IAudioService audio, IModalGate modalGate)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _modalGate = modalGate ?? throw new ArgumentNullException(nameof(modalGate));
        }

        private void Awake()
        {
            if (_content != null)
            {
                _content.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_coinButton != null)
            {
                _coinButton.onClick.AddListener(OnCoinClicked);
            }

            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (_coinButton != null)
            {
                _coinButton.onClick.RemoveListener(OnCoinClicked);
            }

            if (_adButton != null)
            {
                _adButton.onClick.RemoveListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            _showTween?.Kill();
            LowerGate();
        }

        public void Open(AbilityType abilityType)
        {
            _current = abilityType;
            RaiseGate();

            if (_boosterIcon != null)
            {
                _boosterIcon.sprite = IconFor(abilityType);
            }

            if (_priceText != null)
            {
                _priceText.text = _shop.GetCoinPrice(abilityType).ToString();
            }

            if (_coinButton != null)
            {
                _coinButton.interactable = _shop.CanAffordCoins(abilityType);
            }

            Show();
        }

        public void Close()
        {
            if (_content != null)
            {
                _content.SetActive(false);
            }

            LowerGate();
        }

        private void RaiseGate()
        {
            if (_gateOpen || _modalGate == null)
            {
                return;
            }

            _gateOpen = true;
            _modalGate.Open();
        }

        private void LowerGate()
        {
            if (_gateOpen == false || _modalGate == null)
            {
                return;
            }

            _gateOpen = false;
            _modalGate.Close();
        }

        private void Show()
        {
            if (_content != null)
            {
                _content.SetActive(true);
            }

            _showTween?.Kill();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _showTween = DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 1f, _showDuration)
                    .SetUpdate(true);
            }

            if (_panel != null)
            {
                _panel.localScale = Vector3.one * _fromScale;
                _panel.DOScale(1f, _showDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private Sprite IconFor(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Freeze:
                    return _freezeIcon;

                case AbilityType.Crash:
                    return _hammerIcon;

                case AbilityType.Swap:
                    return _swapIcon;

                case AbilityType.Replace:
                    return _replaceIcon;

                default:
                    return null;
            }
        }

        private void PlaySuccess()
        {
            _audio.PlayOneShotSafe(_successSound);
        }

        private void OnCoinClicked()
        {
            if (_shop.BuyForCoins(_current))
            {
                PlaySuccess();
                Close();
            }
        }

        private void OnAdClicked()
        {
            _shop.BuyForAd(_current, OnAdGranted);
        }

        private void OnAdGranted()
        {
            PlaySuccess();
            Close();
        }
    }
}
