using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Common
{
    public class PopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _showDuration = 0.28f;
        [SerializeField] private float _fromScale = 0.8f;

        private Sequence _showSequence;

        public event Action Closed;

        public bool IsShown => _root != null && _root.activeSelf;

        protected virtual void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        protected virtual void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            _showSequence?.Kill();
            _showSequence = null;
        }

        public void InitializePopup(GameObject root, RectTransform content, CanvasGroup canvasGroup, Button closeButton)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _content = content;
            _canvasGroup = canvasGroup;
            _closeButton = closeButton;
        }

        public void Show()
        {
            _root.SetActive(true);
            PlayShowAnimation();
            OnShown();
        }

        public void Hide()
        {
            _showSequence?.Kill();
            _showSequence = null;

            if (_root.activeSelf)
            {
                _root.SetActive(false);
                OnHidden();
            }
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
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

            _showSequence.Join(_content.DOScale(1f, _showDuration).SetEase(Ease.OutBack).SetUpdate(true));
        }

        private void OnCloseClicked()
        {
            Closed?.Invoke();
        }
    }
}
