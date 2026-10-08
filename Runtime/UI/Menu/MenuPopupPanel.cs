using System;
using _Project.Core.Menu;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Menu
{
    public class MenuPopupPanel : MonoBehaviour
    {
        [SerializeField] private MenuPopup _popup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _root;

        [Header("Appear animation")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _showDuration = 0.28f;
        [SerializeField] private float _fromScale = 0.8f;

        private Sequence _showSequence;

        public event Action Closed;

        public MenuPopup Popup => _popup;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void Show()
        {
            _root.SetActive(true);
            PlayShowAnimation();
        }

        public void Hide()
        {
            _showSequence?.Kill();
            _showSequence = null;
            _root.SetActive(false);
        }

        private void PlayShowAnimation()
        {
            _showSequence?.Kill();

            if (_content == null)
            {
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            _content.localScale = Vector3.one * _fromScale;

            _showSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (_canvasGroup != null)
            {
                _showSequence.Join(
                    DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 1f, _showDuration)
                        .SetUpdate(true));
            }

            _showSequence.Join(
                _content.DOScale(1f, _showDuration).SetEase(Ease.OutBack).SetUpdate(true));
        }

        private void OnCloseClicked()
        {
            Closed?.Invoke();
        }
    }
}
