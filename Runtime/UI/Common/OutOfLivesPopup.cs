using System;
using _Project.Core.Advertising;
using _Project.Core.Lives;
using _Project.Core.Shop;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Common
{
    public class OutOfLivesPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _buyInfiniteButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _showDuration = 0.28f;
        [SerializeField] private float _fromScale = 0.7f;

        private IAdvertisingService _ads;
        private ILivesService _lives;
        private IShopEntry _shopEntry;

        private Action _onGranted;

        [Inject]
        public void Construct(IAdvertisingService ads, ILivesService lives, IShopEntry shopEntry)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _shopEntry = shopEntry ?? throw new ArgumentNullException(nameof(shopEntry));

            Hide();
        }

        public void Show(Action onGranted)
        {
            _onGranted = onGranted;

            if (_root != null)
            {
                _root.SetActive(true);
            }

            PlayAppear();
        }

        private void PlayAppear()
        {
            if (_panel != null)
            {
                _panel.DOKill();
                _panel.localScale = Vector3.one * _fromScale;
                _panel.DOScale(1f, _showDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.alpha = 0f;
                DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 1f, _showDuration * 0.6f)
                    .SetTarget(_canvasGroup)
                    .SetUpdate(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
            }

            if (_buyInfiniteButton != null)
            {
                _buyInfiniteButton.onClick.AddListener(OnBuyInfiniteClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (_adButton != null)
            {
                _adButton.onClick.RemoveListener(OnAdClicked);
            }

            if (_buyInfiniteButton != null)
            {
                _buyInfiniteButton.onClick.RemoveListener(OnBuyInfiniteClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnAdClicked()
        {
            _ads.ShowRewarded("free_lives", OnAdRewarded, null);
        }

        private void OnAdRewarded()
        {
            _lives.Fill();
            Grant();
        }

        private void OnBuyInfiniteClicked()
        {
            _onGranted = null;
            Hide();
            _shopEntry.OpenShop();
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        private void Grant()
        {
            Action callback = _onGranted;
            _onGranted = null;

            Hide();

            callback?.Invoke();
        }
    }
}
