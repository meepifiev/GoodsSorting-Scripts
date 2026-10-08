using System;
using DG.Tweening;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfItemView : MonoBehaviour
    {
        private const float InactivityHintScaleMultiplier = 1.06f;
        private const float InactivityHintPulseDuration = 0.25f;

        private Vector3 _slotLocalScale;
        private Vector3 _inactivityHintInitialScale;
        private bool _hasSlotLocalScale;
        private Sequence _inactivityHintSequence;

        public event Action InactivityHintRequested;
        public event Action Destroyed;

        public ShelfCellView Cell { get; private set; }
        public ShelfLayerView Layer { get; private set; }
        public Transform Slot { get; private set; }
        public int ItemId { get; private set; }
        public int LayerIndex { get; private set; }
        public bool IsRemoving { get; private set; }

        public bool IsFrontLayer => LayerIndex == 0;
        public bool CanDrag => IsRemoving == false &&
                               Cell != null &&
                               IsFrontLayer &&
                               Cell.IsResolvingMatch == false;
        public Vector3 SlotLocalScale => _hasSlotLocalScale
            ? _slotLocalScale
            : transform.localScale;

        private void OnDestroy()
        {
            StopInactivityHint();
            Destroyed?.Invoke();
        }

        public void SetItemId(int itemId)
        {
            ItemId = itemId;
        }

        public void MarkRemoving()
        {
            StopInactivityHint();
            IsRemoving = true;
        }

        public void RequestInactivityHint()
        {
            if (IsRemoving)
            {
                return;
            }

            StopInactivityHint();
            _inactivityHintInitialScale = transform.localScale;
            _inactivityHintSequence = DOTween.Sequence()
                .Append(
                    transform
                        .DOScale(
                            _inactivityHintInitialScale * InactivityHintScaleMultiplier,
                            InactivityHintPulseDuration)
                        .SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
            InactivityHintRequested?.Invoke();
        }

        public void StopInactivityHint()
        {
            if (_inactivityHintSequence == null)
            {
                return;
            }

            _inactivityHintSequence.Kill();
            _inactivityHintSequence = null;
            transform.localScale = _inactivityHintInitialScale;
        }

        public void Bind(
            ShelfCellView cell,
            ShelfLayerView layer,
            Transform slot,
            int layerIndex)
        {
            if (_hasSlotLocalScale == false)
            {
                _slotLocalScale = transform.localScale;
                _hasSlotLocalScale = true;
            }

            BindWithoutCapturingScale(
                cell,
                layer,
                slot,
                layerIndex);
        }

        public void AttachToSlot(
            ShelfCellView cell,
            ShelfLayerView layer,
            Transform slot,
            int layerIndex)
        {
            if (_hasSlotLocalScale == false)
            {
                _slotLocalScale = transform.localScale;
                _hasSlotLocalScale = true;
            }

            transform.SetParent(slot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _slotLocalScale;

            BindWithoutCapturingScale(
                cell,
                layer,
                slot,
                layerIndex);
        }

        private void BindWithoutCapturingScale(
            ShelfCellView cell,
            ShelfLayerView layer,
            Transform slot,
            int layerIndex)
        {
            Cell = cell;
            Layer = layer;
            Slot = slot;
            LayerIndex = layerIndex;
        }
    }
}
