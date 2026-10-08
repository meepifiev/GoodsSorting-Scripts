using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class CoinFlyEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform _layer;
        [SerializeField] private Sprite _coinSprite;
        [Tooltip("Optional trailing sprite left behind flying coins. Falls back to the coin sprite.")]
        [SerializeField] private Sprite _trailSprite;
        [SerializeField] private ParticleSystem _arrivalBurst;
        [SerializeField] private int _count = 12;
        [SerializeField] private float _duration = 0.62f;
        [SerializeField] private float _stagger = 0.045f;
        [SerializeField] private float _spread = 90f;
        [SerializeField] private float _size = 70f;
        [Tooltip("Spin the flying coins/gems by flipping them through the edge (matches the reference).")]
        [SerializeField] private bool _flip = true;
        [Tooltip("Seconds for a half coin-flip (edge to edge). Lower = faster spin.")]
        [SerializeField] private float _flipDuration = 0.18f;

        [Header("3D coin (Spine)")]
        [Tooltip("Optional Spine coin that plays its own 'Flip' spin (the reference's 3D coin). When set, flying coins use this instead of the flat sprite. Leave empty for gems/fallback.")]
        [SerializeField] private SkeletonGraphic _coinSpinePrefab;
        [Tooltip("Visual scale multiplier for the Spine coin.")]
        [SerializeField] private float _spineScale = 1f;

        [Header("Spin flipbook (rotation frames)")]
        [Tooltip("Optional sprite sheet of rotation frames (single row). When set, the flying item cycles these frames for a real 3D spin instead of the flat flip.")]
        [SerializeField] private Texture2D _spinSheet;
        [SerializeField] private int _spinColumns = 8;
        [SerializeField] private int _spinRows = 1;
        [SerializeField] private int _spinFrameCount = 8;
        [SerializeField] private float _spinFps = 16f;

        private Sprite[] _spinFrames;

        public float TotalDuration => _duration + _stagger * _count + 0.12f;

        public bool Play(RectTransform source, RectTransform target)
        {
            if (_layer == null || source == null || target == null)
            {
                return false;
            }

            if (_coinSprite == null && _coinSpinePrefab == null)
            {
                return false;
            }

            Vector2 start = WorldToLayer(source.position);
            Vector2 end = WorldToLayer(target.position);

            for (int i = 0; i < _count; i++)
            {
                SpawnCoin(start, end, i, target);
            }

            return true;
        }

        private void SpawnCoin(Vector2 start, Vector2 end, int index, RectTransform target)
        {
            if (_coinSpinePrefab != null)
            {
                SpawnSpineCoin(start, end, index, target);
                return;
            }

            var go = new GameObject("RewardCoin", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer, false);
            rt.sizeDelta = new Vector2(_size, _size);
            rt.anchoredPosition = start;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            Sprite[] frames = SpinFrames();
            bool spin = frames != null && frames.Length > 0;
            image.sprite = spin ? frames[0] : _coinSprite;

            float angle = (index / (float)Mathf.Max(1, _count)) * Mathf.PI * 2f;
            Vector2 control = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spread;
            float delay = index * _stagger;

            if (spin)
            {
                rt.localScale = Vector3.one;
                StartSpin(image, go, delay);
            }
            else if (_flip)
            {
                rt.localScale = Vector3.one;
                rt.DOScaleX(-1f, Mathf.Max(0.01f, _flipDuration))
                    .From(1f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(UnityEngine.Random.Range(0f, _flipDuration))
                    .SetLink(go);
            }
            else
            {
                rt.localScale = Vector3.zero;
                rt.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack).SetDelay(delay).SetLink(go);
            }

            int[] frame = { 0 };
            float t = 0f;
            DOTween.To(() => t, x =>
                {
                    t = x;
                    float inv = 1f - t;
                    rt.anchoredPosition = inv * inv * start + 2f * inv * t * control + t * t * end;

                    frame[0]++;
                    if (frame[0] % 2 == 0)
                    {
                        SpawnTrail(rt.anchoredPosition);
                    }
                }, 1f, _duration)
                .SetEase(Ease.InOutSine)
                .SetDelay(delay)
                .SetLink(go)
                .OnComplete(() =>
                {
                    if (target != null)
                    {
                        target.DOKill(true);
                        target.DOPunchScale(Vector3.one * 0.16f, 0.22f, 6, 0.6f).SetLink(target.gameObject);
                    }

                    if (_arrivalBurst != null)
                    {
                        _arrivalBurst.Play(true);
                    }

                    Destroy(go);
                });
        }

        private void SpawnSpineCoin(Vector2 start, Vector2 end, int index, RectTransform target)
        {
            SkeletonGraphic coin = Instantiate(_coinSpinePrefab, _layer);
            coin.raycastTarget = false;

            RectTransform rt = coin.rectTransform;
            rt.anchoredPosition = start;
            rt.localScale = Vector3.one * _spineScale;

            GameObject go = coin.gameObject;

            float angle = (index / (float)Mathf.Max(1, _count)) * Mathf.PI * 2f;
            Vector2 control = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spread;
            float delay = index * _stagger;

            float t = 0f;
            DOTween.To(() => t, x =>
                {
                    t = x;
                    float inv = 1f - t;
                    rt.anchoredPosition = inv * inv * start + 2f * inv * t * control + t * t * end;
                }, 1f, _duration)
                .SetEase(Ease.InOutSine)
                .SetDelay(delay)
                .SetLink(go)
                .OnComplete(() =>
                {
                    if (target != null)
                    {
                        target.DOKill(true);
                        target.DOPunchScale(Vector3.one * 0.16f, 0.22f, 6, 0.6f).SetLink(target.gameObject);
                    }

                    if (_arrivalBurst != null)
                    {
                        _arrivalBurst.Play(true);
                    }

                    Destroy(go);
                });
        }

        private Sprite[] SpinFrames()
        {
            if (_spinFrames != null)
            {
                return _spinFrames;
            }

            if (_spinSheet == null || _spinColumns <= 0 || _spinRows <= 0 || _spinFrameCount <= 0)
            {
                return null;
            }

            float frameWidth = (float)_spinSheet.width / _spinColumns;
            float frameHeight = (float)_spinSheet.height / _spinRows;
            _spinFrames = new Sprite[_spinFrameCount];

            for (int i = 0; i < _spinFrameCount; i++)
            {
                int column = i % _spinColumns;
                int row = i / _spinColumns;
                Rect rect = new Rect(column * frameWidth, _spinSheet.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                _spinFrames[i] = Sprite.Create(_spinSheet, rect, new Vector2(0.5f, 0.5f), 100f);
            }

            return _spinFrames;
        }

        private void StartSpin(Image image, GameObject go, float delay)
        {
            Sprite[] frames = _spinFrames;
            int count = frames.Length;
            float total = _duration + delay + 0.25f;
            float endValue = Mathf.Max(1f, _spinFps * total);

            DOTween.To(() => 0f, x =>
                {
                    if (image == null)
                    {
                        return;
                    }

                    int idx = ((int)x) % count;

                    if (idx < 0)
                    {
                        idx += count;
                    }

                    image.sprite = frames[idx];
                }, endValue, total)
                .SetEase(Ease.Linear)
                .SetLink(go);
        }

        private void SpawnTrail(Vector2 position)
        {
            Sprite sprite = _trailSprite != null ? _trailSprite : _coinSprite;
            if (sprite == null || _layer == null)
            {
                return;
            }

            var go = new GameObject("CoinTrail", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer, false);
            rt.anchoredPosition = position;

            float size = _size * UnityEngine.Random.Range(0.45f, 0.7f);
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;

            rt.SetAsFirstSibling();

            Sequence sequence = DOTween.Sequence().SetLink(go);
            sequence.Append(rt.DOScale(0f, 0.3f).SetEase(Ease.InSine));
            sequence.Join(DOTween.To(
                () => image.color.a,
                a =>
                {
                    Color color = image.color;
                    color.a = a;
                    image.color = color;
                },
                0f,
                0.3f));
            sequence.OnComplete(() => Destroy(go));
        }

        private Vector2 WorldToLayer(Vector3 world)
        {
            Canvas canvas = _layer != null ? _layer.GetComponentInParent<Canvas>() : null;
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, uiCamera, out Vector2 local);
            return local;
        }
    }
}
