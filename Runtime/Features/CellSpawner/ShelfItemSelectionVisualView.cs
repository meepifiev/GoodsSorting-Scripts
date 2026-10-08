using DG.Tweening;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfItemSelectionVisualView : MonoBehaviour
    {
        private Vector3 _selectedLocalOffset = new(0f, -0.12f, 0f);

        private Vector3 _selectedShadowOffset = new(0.3f, -0.35f, 0f);

        private float _selectedScaleMultiplier = 1.1f;

        [SerializeField]
        private float _transitionDuration = 0.14f;

        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private Vector3 _currentDragOffset;
        private bool _hasBaseTransform;
        private bool _isSelected;

        public Vector3 CurrentDragOffset => _currentDragOffset;

        public void SetSelected(
            bool isSelected,
            bool applyPositionToTransform)
        {
            SetSelected(
                isSelected,
                applyPositionToTransform,
                false);
        }

        public void SetSelected(
            bool isSelected,
            bool applyPositionToTransform,
            bool useCurrentTransformAsBase)
        {
            CaptureBaseTransformIfNeeded(
                isSelected == false || useCurrentTransformAsBase,
                useCurrentTransformAsBase);
            _isSelected = isSelected;
            DOTween.Kill(transform);
            DOTween.Kill(this);

            Vector3 targetScale = isSelected
                ? _baseLocalScale * _selectedScaleMultiplier
                : _baseLocalScale;
            Vector3 targetDragOffset = isSelected
                ? _selectedLocalOffset
                : Vector3.zero;

            if (applyPositionToTransform)
            {
                Vector3 targetPosition = isSelected
                    ? _baseLocalPosition + _selectedLocalOffset
                    : _baseLocalPosition;

                transform
                    .DOLocalMove(targetPosition, _transitionDuration)
                    .SetEase(Ease.OutSine)
                    .SetTarget(transform);
            }
            else
            {
                DOTween.To(
                        () => _currentDragOffset,
                        value => { _currentDragOffset = value; },
                        targetDragOffset,
                        _transitionDuration)
                    .SetEase(Ease.OutSine)
                    .SetTarget(this);
            }

            transform
                .DOScale(targetScale, _transitionDuration)
                .SetEase(Ease.OutSine)
                .SetTarget(transform)
                .OnComplete(
                    () =>
                    {
                        if (isSelected == false)
                        {
                            _hasBaseTransform = false;
                            _isSelected = false;
                        }
                    });

            ShelfItemShadowView shadowView = GetComponent<ShelfItemShadowView>();

            if (shadowView != null)
            {
                shadowView.SetSelectionOffset(
                    isSelected ? _selectedShadowOffset : Vector3.zero,
                    _transitionDuration);
            }
        }

        public void PrepareForDrag()
        {
            CaptureBaseTransformIfNeeded();
            DOTween.Kill(transform);
            DOTween.Kill(this);

            if (_isSelected)
            {
                transform.localPosition = _baseLocalPosition;
                transform.localScale = _baseLocalScale * _selectedScaleMultiplier;
                _currentDragOffset = _selectedLocalOffset;
                return;
            }

            _currentDragOffset = Vector3.zero;
        }

        private void CaptureBaseTransformIfNeeded(
            bool force = false,
            bool useCurrentTransformAsBase = false)
        {
            if (_hasBaseTransform && force == false)
                return;

            ShelfItemView itemView = GetComponent<ShelfItemView>();

            if (useCurrentTransformAsBase)
            {
                _baseLocalPosition = transform.localPosition;
                _baseLocalScale = transform.localScale;
                _currentDragOffset = Vector3.zero;
                _hasBaseTransform = true;
                return;
            }

            _baseLocalPosition = itemView != null && itemView.Slot == transform.parent
                ? Vector3.zero
                : transform.localPosition;
            _baseLocalScale = itemView != null
                ? itemView.SlotLocalScale
                : transform.localScale;
            _currentDragOffset = Vector3.zero;
            _hasBaseTransform = true;
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            DOTween.Kill(this);
        }
    }
}
