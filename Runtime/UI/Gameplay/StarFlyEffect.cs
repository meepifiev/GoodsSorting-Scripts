using System;
using _Project.Core.Combo;
using _Project.Features.Cameras;
using _Project.Features.CellSpawner;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class StarFlyEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform _flyRoot;
        [SerializeField] private Sprite _starSprite;
        [SerializeField] private Sprite _sparkSprite;
        [SerializeField] private RectTransform _target;
        [SerializeField] private StarsView _starsView;
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector2 _starSize = new Vector2(90f, 90f);
        [SerializeField] private float _flyDuration = 0.6f;
        [SerializeField] private float _stagger = 0.08f;
        [SerializeField] private float _curveHeight = 250f;
        [SerializeField] private float _spread = 60f;

        private LevelShelfSpawner _spawner;
        private IComboService _comboService;
        private IMainCameraProvider _cameraProvider;
        private int _displayed;

        public event Action StarReceived;

        [Inject]
        private void Construct(LevelShelfSpawner spawner, IComboService comboService, IMainCameraProvider cameraProvider)
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _comboService = comboService ?? throw new ArgumentNullException(nameof(comboService));
            _cameraProvider = cameraProvider ?? throw new ArgumentNullException(nameof(cameraProvider));
            _spawner.LevelSpawned += OnLevelSpawned;
            _spawner.LevelCleared += ResetCounters;
            _comboService.MatchScored += OnMatchScored;
            ResetCounters();
        }

        private void OnDestroy()
        {
            if (_spawner != null)
            {
                _spawner.LevelSpawned -= OnLevelSpawned;
                _spawner.LevelCleared -= ResetCounters;
            }

            if (_comboService != null)
            {
                _comboService.MatchScored -= OnMatchScored;
            }
        }

        private void OnLevelSpawned(int itemCount)
        {
            ResetCounters();
        }

        private void ResetCounters()
        {
            _displayed = 0;

            if (_starsView != null)
            {
                _starsView.SetStars(0);
            }
        }

        private void OnMatchScored(Vector3 worldPosition, int stars)
        {
            if (stars <= 0)
            {
                return;
            }

            Vector2 source = WorldToLocal(worldPosition);
            Vector2 targetLocal = TargetLocal();

            for (int i = 0; i < stars; i++)
            {
                FlyOne(source, targetLocal, i * _stagger);
            }
        }

        private void FlyOne(Vector2 source, Vector2 target, float delay)
        {
            if (_starSprite == null || _flyRoot == null)
            {
                return;
            }

            GameObject go = new GameObject("FlyStar", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_flyRoot, false);
            rt.sizeDelta = _starSize;

            Image image = go.GetComponent<Image>();
            image.sprite = _starSprite;
            image.raycastTarget = false;
            image.preserveAspect = true;

            Vector2 start = source + new Vector2(
                UnityEngine.Random.Range(-_spread, _spread),
                UnityEngine.Random.Range(-_spread, _spread));
            Vector2 control = (start + target) * 0.5f + new Vector2(0f, _curveHeight);

            rt.anchoredPosition = start;
            rt.localScale = Vector3.one * 0.3f;

            rt.DOScale(1f, 0.2f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(go);

            int[] frame = { 0 };
            DOVirtual.Float(0f, 1f, _flyDuration,
                    t =>
                    {
                        Vector2 pos = QuadraticBezier(start, control, target, t);
                        rt.anchoredPosition = pos;
                        frame[0]++;

                        if (_sparkSprite != null && frame[0] % 2 == 0)
                        {
                            SpawnSpark(pos);
                        }
                    })
                .SetDelay(delay)
                .SetEase(Ease.InOutSine)
                .SetLink(go)
                .OnComplete(() =>
                {
                    _displayed++;

                    if (_starsView != null)
                    {
                        _starsView.SetStars(_displayed);
                        _starsView.Bump();
                    }

                    SpawnArrivalBurst(target);
                    StarReceived?.Invoke();
                    Destroy(go);
                });
        }

        private void SpawnSpark(Vector2 position)
        {
            GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(_flyRoot, false);
            rt.anchoredPosition = position + new Vector2(
                UnityEngine.Random.Range(-14f, 14f),
                UnityEngine.Random.Range(-14f, 14f));

            float size = UnityEngine.Random.Range(16f, 34f);
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            Image image = go.GetComponent<Image>();
            image.sprite = _sparkSprite;
            image.raycastTarget = false;
            image.preserveAspect = true;

            rt.SetAsFirstSibling();

            Sequence sequence = DOTween.Sequence().SetLink(go);
            sequence.Append(rt.DOScale(0f, 0.35f).SetEase(Ease.InSine));
            sequence.Join(DOTween.To(
                () => image.color.a,
                a =>
                {
                    Color color = image.color;
                    color.a = a;
                    image.color = color;
                },
                0f,
                0.35f));
            sequence.OnComplete(() => Destroy(go));
        }

        private void SpawnArrivalBurst(Vector2 position)
        {
            if (_sparkSprite == null)
            {
                return;
            }

            const int count = 7;

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("BurstSpark", typeof(RectTransform), typeof(Image));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(_flyRoot, false);
                rt.anchoredPosition = position;

                float size = UnityEngine.Random.Range(20f, 42f);
                rt.sizeDelta = new Vector2(size, size);
                rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

                Image image = go.GetComponent<Image>();
                image.sprite = _sparkSprite;
                image.raycastTarget = false;
                image.preserveAspect = true;

                float angle = (360f / count) * i + UnityEngine.Random.Range(-18f, 18f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 destination = position + dir * UnityEngine.Random.Range(55f, 95f);

                Sequence sequence = DOTween.Sequence().SetLink(go);
                sequence.Append(DOTween.To(
                        () => rt.anchoredPosition,
                        p => rt.anchoredPosition = p,
                        destination,
                        0.4f)
                    .SetEase(Ease.OutCubic));
                sequence.Join(rt.DOScale(0f, 0.4f).SetEase(Ease.InSine));
                sequence.Join(DOTween.To(
                    () => image.color.a,
                    a =>
                    {
                        Color color = image.color;
                        color.a = a;
                        image.color = color;
                    },
                    0f,
                    0.4f));
                sequence.OnComplete(() => Destroy(go));
            }
        }

        private static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private Vector2 WorldToLocal(Vector3 worldPosition)
        {
            Camera camera = _camera != null ? _camera : _cameraProvider.Camera;
            Vector2 screen = camera != null
                ? (Vector2)camera.WorldToScreenPoint(worldPosition)
                : (Vector2)worldPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_flyRoot, screen, null, out Vector2 local);
            return local;
        }

        private Vector2 TargetLocal()
        {
            if (_target == null)
            {
                return Vector2.zero;
            }

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, _target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_flyRoot, screen, null, out Vector2 local);
            return local;
        }
    }
}
