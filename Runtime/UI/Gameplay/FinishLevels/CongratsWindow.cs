using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class CongratsWindow : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _confetti;
        [SerializeField] private RectTransform _title;
        [SerializeField] private Button _tapButton;
        [SerializeField] private float _autoProceedDelay = 2.2f;

        [Header("Star confetti")]
        [SerializeField] private RectTransform _confettiLayer;
        [SerializeField] private Sprite _confettiStarSprite;
        [SerializeField] private int _confettiCount = 48;
        [SerializeField] private float _confettiSize = 48f;
        [SerializeField] private float _confettiFallDuration = 2.4f;

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.27f, 0.27f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.86f, 0.25f),
            new Color(0.35f, 0.85f, 0.35f),
            new Color(0.3f, 0.8f, 1f),
            new Color(0.4f, 0.5f, 1f),
            new Color(0.7f, 0.45f, 1f),
            new Color(1f, 0.45f, 0.85f)
        };

        private Action _onProceed;
        private bool _proceeded;

        public void Show(Action onProceed)
        {
            _onProceed = onProceed;
            _proceeded = false;
            gameObject.SetActive(true);

            if (_confetti != null)
            {
                _confetti.Play(true);
            }

            PlayStarConfetti();

            if (_title != null)
            {
                _title.DOKill();
                _title.localScale = Vector3.one * 0.3f;
                _title.DOScale(Vector3.one, 0.42f).SetEase(Ease.OutBack).SetLink(_title.gameObject);
            }

            CancelInvoke();
            Invoke(nameof(Proceed), _autoProceedDelay);
        }

        public void Hide()
        {
            CancelInvoke();
            gameObject.SetActive(false);
        }

        private void PlayStarConfetti()
        {
            if (_confettiStarSprite == null || _confettiLayer == null)
            {
                return;
            }

            Rect rect = _confettiLayer.rect;
            float halfWidth = rect.width * 0.5f;
            float top = rect.height * 0.5f;

            for (int i = 0; i < _confettiCount; i++)
            {
                SpawnConfettiStar(halfWidth, top);
            }
        }

        private void SpawnConfettiStar(float halfWidth, float top)
        {
            var go = new GameObject("ConfettiStar", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_confettiLayer, false);

            float size = _confettiSize * UnityEngine.Random.Range(0.7f, 1.25f);
            rt.sizeDelta = new Vector2(size, size);

            var start = new Vector2(
                UnityEngine.Random.Range(-halfWidth, halfWidth),
                top + UnityEngine.Random.Range(40f, 420f));
            rt.anchoredPosition = start;

            var image = go.GetComponent<Image>();
            image.sprite = _confettiStarSprite;
            image.color = ConfettiColors[UnityEngine.Random.Range(0, ConfettiColors.Length)];
            image.raycastTarget = false;
            image.preserveAspect = true;

            float duration = _confettiFallDuration * UnityEngine.Random.Range(0.75f, 1.2f);
            float endY = -top - 120f;
            float swayAmp = UnityEngine.Random.Range(40f, 150f);
            float swayFreq = UnityEngine.Random.Range(1.5f, 3f);
            float startRot = UnityEngine.Random.Range(0f, 360f);
            float spin = UnityEngine.Random.Range(-540f, 540f);
            float delay = UnityEngine.Random.Range(0f, 0.6f);

            float t = 0f;
            DOTween.To(() => t, x =>
                {
                    t = x;
                    float y = Mathf.Lerp(start.y, endY, t * t);
                    float x2 = start.x + Mathf.Sin(t * Mathf.PI * swayFreq) * swayAmp;
                    rt.anchoredPosition = new Vector2(x2, y);
                    rt.localRotation = Quaternion.Euler(0f, 0f, startRot + spin * t);

                    float alpha = t > 0.82f ? Mathf.InverseLerp(1f, 0.82f, t) : 1f;
                    Color color = image.color;
                    color.a = alpha;
                    image.color = color;
                }, 1f, duration)
                .SetEase(Ease.Linear)
                .SetDelay(delay)
                .SetLink(go)
                .OnComplete(() => Destroy(go));
        }

        private void OnEnable()
        {
            if (_tapButton != null)
            {
                _tapButton.onClick.AddListener(Proceed);
            }
        }

        private void OnDisable()
        {
            if (_tapButton != null)
            {
                _tapButton.onClick.RemoveListener(Proceed);
            }

            CancelInvoke();
        }

        private void Proceed()
        {
            if (_proceeded)
            {
                return;
            }

            _proceeded = true;
            CancelInvoke();

            Action cb = _onProceed;
            _onProceed = null;

            if (cb != null)
            {
                cb.Invoke();
            }
        }
    }
}
