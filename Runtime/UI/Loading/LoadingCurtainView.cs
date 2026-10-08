using _Project.Core.SceneManagement;
using UnityEngine;

namespace _Project.UI.Loading
{
    public class LoadingCurtainView : MonoBehaviour, ILoadingCurtain
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _mascot;
        [SerializeField] private RectTransform[] _dots;
        [SerializeField] private float _bounceHeight = 20f;
        [SerializeField] private float _bounceSpeed = 6f;
        [SerializeField] private float _dotDelay = 0.16f;
        [SerializeField] private float _mascotPulse = 0.04f;
        [SerializeField] private float _mascotPulseSpeed = 2.2f;

        private bool _shown;
        private float _time;
        private float[] _dotBaseY;
        private Vector3 _mascotBaseScale = Vector3.one;

        private void Awake()
        {
            Cache();
            HideImmediate();
        }

        public void Show()
        {
            _shown = true;
            _time = 0f;

            if (_canvas != null)
                _canvas.enabled = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            _shown = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            ResetAnimation();

            if (_canvas != null)
                _canvas.enabled = false;
        }

        private void Update()
        {
            if (_shown == false)
                return;

            _time += Time.unscaledDeltaTime;

            if (_mascot != null)
            {
                float pulse = 1f + Mathf.Sin(_time * _mascotPulseSpeed) * _mascotPulse;
                _mascot.localScale = _mascotBaseScale * pulse;
            }

            if (_dots == null || _dotBaseY == null)
                return;

            float t = _time * _bounceSpeed;

            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] == null)
                    continue;

                float phase = t - i * _dotDelay * _bounceSpeed;
                float offset = Mathf.Max(0f, Mathf.Sin(phase)) * _bounceHeight;

                Vector2 position = _dots[i].anchoredPosition;
                position.y = _dotBaseY[i] + offset;
                _dots[i].anchoredPosition = position;
            }
        }

        private void Cache()
        {
            if (_mascot != null)
                _mascotBaseScale = _mascot.localScale;

            if (_dots == null)
                return;

            _dotBaseY = new float[_dots.Length];

            for (int i = 0; i < _dots.Length; i++)
                _dotBaseY[i] = _dots[i] != null ? _dots[i].anchoredPosition.y : 0f;
        }

        private void ResetAnimation()
        {
            if (_mascot != null)
                _mascot.localScale = _mascotBaseScale;

            if (_dots == null || _dotBaseY == null)
                return;

            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] == null)
                    continue;

                Vector2 position = _dots[i].anchoredPosition;
                position.y = _dotBaseY[i];
                _dots[i].anchoredPosition = position;
            }
        }

        private void HideImmediate()
        {
            _shown = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_canvas != null)
                _canvas.enabled = false;
        }
    }
}
