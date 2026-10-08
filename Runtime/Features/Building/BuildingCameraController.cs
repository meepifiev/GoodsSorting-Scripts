using DG.Tweening;
using UnityEngine;

namespace _Project.Features.Building
{
    [RequireComponent(typeof(Camera))]
    public class BuildingCameraController : MonoBehaviour
    {
        private const float WheelZoomStep = 0.6f;
        private const float DragStartThresholdPixels = 8f;
        private const float TravelZoomDuration = 0.5f;
        private const float TravelMoveDuration = 0.8f;

        [SerializeField] private Rect _worldBounds = new Rect(-20f, -20f, 40f, 40f);
        [SerializeField] private float _minOrthographicSize = 3f;
        [SerializeField] private float _maxOrthographicSize = 8f;
        [SerializeField] private float _entryOrthographicSize = 6f;
        [SerializeField] private float _travelOrthographicSize = 9f;
        [SerializeField] private float _travelDelay = 1f;
        [SerializeField] private float _inertiaDamping = 8f;

        private Camera _camera;
        private bool _dragging;
        private bool _pressed;
        private Vector2 _pressScreenPosition;
        private Vector3 _lastPointerWorld;
        private Vector3 _velocity;
        private float _lastPinchDistance;
        private bool _inputEnabled = true;
        private Tween _moveTween;

        public Camera Camera => _camera;
        public bool IsDragging => _dragging;
        public Rect WorldBounds => _worldBounds;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_inputEnabled)
            {
                if (Input.touchCount >= 2)
                {
                    HandlePinch();
                }
                else
                {
                    HandleDrag();
                    HandleWheel();
                }
            }

            ApplyInertia();
            ClampToBounds();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (enabled == false)
            {
                _dragging = false;
                _pressed = false;
            }
        }

        public void SetWorldBounds(Rect bounds)
        {
            _worldBounds = bounds;
            ClampToBounds();
        }

        public void SetZoomRange(float minSize, float maxSize)
        {
            _minOrthographicSize = minSize;
            _maxOrthographicSize = maxSize;
        }

        public void SetEntryZoom(float size)
        {
            _entryOrthographicSize = size;
        }

        public void SetTravelZoom(float size, float delay)
        {
            _travelOrthographicSize = size;
            _travelDelay = delay;
        }

        public void TravelTo(Vector2 worldPosition)
        {
            KillMove();
            _velocity = Vector3.zero;
            float entrySize = Mathf.Clamp(_entryOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
            float travelSize = Mathf.Clamp(_travelOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
            Vector2 clamped = ClampPosition(worldPosition, entrySize);
            Vector3 target = new Vector3(clamped.x, clamped.y, transform.position.z);

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.AppendInterval(_travelDelay);
            sequence.Append(DOTween.To(() => _camera.orthographicSize, SetOrthographicSize, travelSize, TravelZoomDuration).SetEase(Ease.InOutSine));
            sequence.Append(transform.DOMove(target, TravelMoveDuration).SetEase(Ease.InOutSine));
            sequence.Append(DOTween.To(() => _camera.orthographicSize, SetOrthographicSize, entrySize, TravelZoomDuration).SetEase(Ease.InOutSine));
            _moveTween = sequence;
        }

        public void ResetView(Vector2 focus)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _camera.orthographicSize = Mathf.Clamp(_entryOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
            FocusOn(focus);
        }

        public void FocusOn(Vector2 worldPosition)
        {
            KillMove();
            Vector3 position = transform.position;
            position.x = worldPosition.x;
            position.y = worldPosition.y;
            transform.position = position;
            _velocity = Vector3.zero;
            ClampToBounds();
        }

        public void MoveTo(Vector2 worldPosition, float duration)
        {
            KillMove();
            _velocity = Vector3.zero;
            Vector3 target = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _moveTween = transform.DOMove(target, duration).SetEase(Ease.InOutSine).SetLink(gameObject);
        }

        private void KillMove()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }

        private void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                KillMove();
                _pressed = true;
                _dragging = false;
                _pressScreenPosition = Input.mousePosition;
                _lastPointerWorld = PointerWorld();
                _velocity = Vector3.zero;
            }

            if (_pressed && Input.GetMouseButton(0))
            {
                Vector2 screenPosition = Input.mousePosition;

                if (_dragging == false && Vector2.Distance(screenPosition, _pressScreenPosition) >= DragStartThresholdPixels)
                {
                    _dragging = true;
                    _lastPointerWorld = PointerWorld();
                }

                if (_dragging)
                {
                    Vector3 pointerWorld = PointerWorld();
                    Vector3 delta = _lastPointerWorld - pointerWorld;
                    transform.position += delta;
                    _velocity = Time.deltaTime > 0f ? delta / Time.deltaTime : Vector3.zero;
                    _lastPointerWorld = PointerWorld();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _pressed = false;
                _dragging = false;
            }
        }

        private void HandleWheel()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            Vector3 before = PointerWorld();
            SetOrthographicSize(_camera.orthographicSize - scroll * WheelZoomStep);
            Vector3 after = PointerWorld();
            transform.position += before - after;
        }

        private void HandlePinch()
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            float distance = Vector2.Distance(first.position, second.position);

            if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began || _lastPinchDistance <= 0f)
            {
                _lastPinchDistance = distance;
                _dragging = false;
                _pressed = false;
                return;
            }

            if (distance <= 0f || _lastPinchDistance <= 0f)
            {
                return;
            }

            Vector2 center = (first.position + second.position) * 0.5f;
            Vector3 before = _camera.ScreenToWorldPoint(center);
            SetOrthographicSize(_camera.orthographicSize * (_lastPinchDistance / distance));
            Vector3 after = _camera.ScreenToWorldPoint(center);
            transform.position += before - after;
            _lastPinchDistance = distance;
            _velocity = Vector3.zero;
        }

        private void ApplyInertia()
        {
            if (_dragging || _velocity.sqrMagnitude < 0.0001f)
            {
                if (_dragging == false)
                {
                    _velocity = Vector3.zero;
                }

                return;
            }

            transform.position += _velocity * Time.deltaTime;
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, _inertiaDamping * Time.deltaTime);
        }

        private void SetOrthographicSize(float size)
        {
            _camera.orthographicSize = Mathf.Clamp(size, _minOrthographicSize, _maxOrthographicSize);
        }

        private void ClampToBounds()
        {
            if (_camera == null)
            {
                return;
            }

            Vector2 clamped = ClampPosition(transform.position, _camera.orthographicSize);
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }

        private Vector2 ClampPosition(Vector2 position, float orthographicSize)
        {
            float halfHeight = orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float minX = _worldBounds.xMin + halfWidth;
            float maxX = _worldBounds.xMax - halfWidth;
            float minY = _worldBounds.yMin + halfHeight;
            float maxY = _worldBounds.yMax - halfHeight;

            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : _worldBounds.center.x;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : _worldBounds.center.y;
            return position;
        }

        private Vector3 PointerWorld()
        {
            Vector3 world = _camera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            return world;
        }
    }
}
