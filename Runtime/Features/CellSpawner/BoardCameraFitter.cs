using System;
using UnityEngine;
using VContainer;

namespace _Project.Features.CellSpawner
{
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField] private Transform _boardRoot;
        [Tooltip("UI that covers the top of the screen. The board is fitted below it.")]
        [SerializeField] private RectTransform _topBound;
        [Tooltip("UI that covers the bottom of the screen. The board is fitted above it.")]
        [SerializeField] private RectTransform _bottomBound;
        [SerializeField] private float _horizontalPadding = 1f;
        [Tooltip("Gap between the board and the panels, as a share of screen height.")]
        [SerializeField] private float _verticalMargin = 0.02f;
        [SerializeField] private float _verticalPadding = 3f;
        [SerializeField] private float _minSize = 3f;
        [SerializeField] private float _maxSize = 60f;

        private Camera _camera;
        private LevelShelfSpawner _levelShelfSpawner;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private float _lastBandBottom;
        private float _lastBandTop;

        [Inject]
        private void Construct(LevelShelfSpawner levelShelfSpawner)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _levelShelfSpawner.LevelSpawned += OnLevelSpawned;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            Fit();
        }

        private void OnDestroy()
        {
            if (_levelShelfSpawner != null)
            {
                _levelShelfSpawner.LevelSpawned -= OnLevelSpawned;
            }
        }

        private void LateUpdate()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                Fit();
                return;
            }

            float screenHeight = Mathf.Max(_camera != null ? _camera.pixelHeight : 1f, 1f);

            if (Mathf.Abs(GetFreeBandBottom(screenHeight) - _lastBandBottom) > 1f
                || Mathf.Abs(GetFreeBandTop(screenHeight) - _lastBandTop) > 1f)
            {
                Fit();
            }
        }

        private void OnLevelSpawned(int itemCount)
        {
            Fit();
        }

        private void Fit()
        {
            if (_camera == null || _boardRoot == null)
            {
                return;
            }

            if (TryGetBoardBounds(out Bounds bounds) == false)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float screenHeight = Mathf.Max(_camera.pixelHeight, 1f);
            float aspect = Mathf.Max(_camera.aspect, 0.0001f);
            float freeBottom = GetFreeBandBottom(screenHeight);
            float freeTop = GetFreeBandTop(screenHeight);
            float freeHeight = freeTop - freeBottom;

            float sizeForWidth = (bounds.extents.x + _horizontalPadding) / aspect;
            float sizeForHeight = freeHeight > 1f
                ? bounds.extents.y * screenHeight / freeHeight
                : bounds.extents.y + _verticalPadding;

            float size = Mathf.Clamp(Mathf.Max(sizeForWidth, sizeForHeight), _minSize, _maxSize);
            _camera.orthographicSize = size;

            float freeCenter = freeHeight > 1f ? (freeBottom + freeTop) * 0.5f : screenHeight * 0.5f;
            float worldPerPixel = size * 2f / screenHeight;
            float centerY = bounds.center.y - (freeCenter - screenHeight * 0.5f) * worldPerPixel;

            Vector3 position = _camera.transform.position;
            _camera.transform.position = new Vector3(bounds.center.x, centerY, position.z);

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastBandBottom = freeBottom;
            _lastBandTop = freeTop;
        }

        private float GetFreeBandBottom(float screenHeight)
        {
            float margin = screenHeight * _verticalMargin;
            return GetScreenEdge(_bottomBound, true, 0f) + margin;
        }

        private float GetFreeBandTop(float screenHeight)
        {
            float margin = screenHeight * _verticalMargin;
            return GetScreenEdge(_topBound, false, screenHeight) - margin;
        }

        private float GetScreenEdge(RectTransform bound, bool takeTopEdge, float fallback)
        {
            if (bound == null || bound.gameObject.activeInHierarchy == false)
            {
                return fallback;
            }

            Vector3[] corners = new Vector3[4];
            bound.GetWorldCorners(corners);
            float bottom = Mathf.Min(corners[0].y, corners[2].y);
            float top = Mathf.Max(corners[0].y, corners[2].y);
            return takeTopEdge ? top : bottom;
        }

        private bool TryGetBoardBounds(out Bounds bounds)
        {
            Renderer[] renderers = _boardRoot.GetComponentsInChildren<Renderer>();
            bool has = false;
            bounds = default;

            foreach (Renderer renderer in renderers)
            {
                if (IsShadow(renderer))
                {
                    continue;
                }

                if (has == false)
                {
                    bounds = renderer.bounds;
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return has;
        }

        private static bool IsShadow(Renderer renderer)
        {
            return renderer.GetComponent<IShelfShadow>() != null;
        }
    }
}
