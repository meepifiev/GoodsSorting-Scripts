using System;
using System.Collections.Generic;
using _Project.Features.Legacy;
using _Project.Features.Legacy.Levels;
using DG.Tweening;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfCellView : MonoBehaviour
    {
        private const int MatchItemCount = 3;

        private static readonly Color BackLayerTint = new(0.3f, 0.3f, 0.3f, 1f);

        [SerializeField]
        private ShelfLayerView _layerPrefab;

        [SerializeField]
        private Transform _layersRoot;

        [SerializeField]
        private Vector3 _layerOffset = new(0f, 0.25f, 0.1f);

        [SerializeField]
        private float _layerMoveDuration = 0.2f;

        [SerializeField]
        private float _layerColorDuration = 0.2f;

        [SerializeField]
        private float _matchPulseScale = 1.06f;

        [SerializeField]
        private float _matchPulseDuration = 0.14f;

        [SerializeField]
        private float _matchShrinkDuration = 0.26f;

        private readonly List<ShelfItemView> _frontLayerItems = new();
        private readonly List<ShelfItemView> _matchingItems = new();

        private ShelfLayerVisuals _visuals;
        private ShelfCellItemSpawner _spawner;
        private CellData _cellData;
        private LegacyItemDatabase _itemDatabase;
        private bool _isResolvingMatch;

        public event Action<int> ItemsRemoved;
        public event Action<Vector3, int> Matched;

        public bool IsResolvingMatch => _isResolvingMatch;

        public void Initialize(
            CellData cellData,
            LegacyItemDatabase itemDatabase,
            ShelfItemShadowConfig shadowConfig)
        {
            _cellData = cellData;
            _itemDatabase = itemDatabase;
            _visuals = new ShelfLayerVisuals(BackLayerTint, _layerOffset, _layerMoveDuration, _layerColorDuration);
            _spawner = new ShelfCellItemSpawner(this, _layerPrefab, _layersRoot, _itemDatabase, _layerOffset, _visuals, shadowConfig);

            ClearLayers();
            _spawner.SpawnLayers(_cellData);
            RefreshLayerVisibility();
        }

        public void RefreshLayerVisibility()
        {
            RefreshLayerVisibility(false);
        }

        public void RefreshLayerVisibility(bool animateLayerMovement)
        {
            if (_layersRoot == null)
                return;

            if (_isResolvingMatch == false)
                RemoveEmptyFrontLayers();

            for (int i = 0; i < _layersRoot.childCount; i++)
            {
                ShelfLayerView layer = _layersRoot.GetChild(i).GetComponent<ShelfLayerView>();

                if (layer == null)
                    continue;

                BindItemsInLayer(layer, i);
                _visuals.ApplyLayer(layer, i, animateLayerMovement);
            }
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (Renderer cellRenderer in renderers)
            {
                if (cellRenderer.GetComponentInParent<ShelfItemView>() != null)
                    continue;

                if (hasBounds == false)
                {
                    bounds = cellRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(cellRenderer.bounds);
            }

            return hasBounds &&
                   worldPoint.x >= bounds.min.x &&
                   worldPoint.x <= bounds.max.x &&
                   worldPoint.y >= bounds.min.y &&
                   worldPoint.y <= bounds.max.y;
        }

        public bool TryAttachItemToFreeFrontSlot(ShelfItemView item)
        {
            if (item == null)
                return false;

            if (TryGetFreeFrontSlot(out ShelfLayerView frontLayer, out Transform freeSlot) == false)
                return false;

            item.AttachToSlot(this, frontLayer, freeSlot, 0);
            _spawner.EnsureItemShadow(item);
            _visuals.RegisterDefaults(item.gameObject);

            return true;
        }

        public bool TryAttachItemToNearestFreeFrontSlot(
            ShelfItemView item,
            Vector3 worldPoint)
        {
            return TryAttachItemToNearestFreeFrontSlot(
                item,
                worldPoint,
                false);
        }

        public bool TryAttachItemToNearestFreeFrontSlot(
            ShelfItemView item,
            Vector3 worldPoint,
            bool preserveWorldPosition)
        {
            if (item == null)
                return false;

            Vector3 itemWorldPosition = item.transform.position;
            Vector3 itemWorldScale = item.transform.lossyScale;

            if (TryGetNearestFreeFrontSlot(
                    worldPoint,
                    out ShelfLayerView frontLayer,
                    out Transform freeSlot) == false)
            {
                return false;
            }

            item.AttachToSlot(this, frontLayer, freeSlot, 0);
            _spawner.EnsureItemShadow(item);

            if (preserveWorldPosition)
            {
                item.transform.position = itemWorldPosition;
                item.transform.localScale = ScaleMath.GetLocalScaleForWorldScale(
                    itemWorldScale,
                    item.transform.parent);
            }

            _visuals.RegisterDefaults(item.gameObject);

            return true;
        }

        public void RemoveEmptyFrontLayers()
        {
            if (_layersRoot == null)
                return;

            while (_layersRoot.childCount > 1)
            {
                ShelfLayerView frontLayer = _layersRoot.GetChild(0).GetComponent<ShelfLayerView>();

                if (frontLayer == null || HasItems(frontLayer))
                    return;

                GameObject layerObject = frontLayer.gameObject;
                layerObject.transform.SetParent(null);

                if (Application.isPlaying)
                    Destroy(layerObject);
                else
                    DestroyImmediate(layerObject);
            }
        }

        public bool CanReplaceItem(int itemId)
        {
            return _itemDatabase != null && _itemDatabase.GetPrefab(itemId) != null;
        }

        public ShelfLayerView GetFrontLayer()
        {
            return TryGetFrontLayer(out ShelfLayerView frontLayer) ? frontLayer : null;
        }

        public ShelfItemView ReplaceFrontItem(ShelfItemView oldItem, int newItemId)
        {
            if (oldItem == null || _isResolvingMatch)
                return null;

            if (oldItem.Cell != this || oldItem.IsFrontLayer == false || oldItem.IsRemoving)
                return null;

            ShelfLayerView layer = oldItem.Layer;
            Transform slot = oldItem.Slot;
            int layerIndex = oldItem.LayerIndex;

            if (layer == null || slot == null)
                return null;

            ShelfItemView newItem = _spawner.SpawnItemInSlot(layer, slot, newItemId, layerIndex);

            if (newItem == null)
                return null;

            oldItem.MarkRemoving();
            Transform oldTransform = oldItem.transform;
            DOTween.Kill(oldTransform);
            oldTransform.SetParent(null);

            if (Application.isPlaying)
                Destroy(oldItem.gameObject);
            else
                DestroyImmediate(oldItem.gameObject);

            _spawner.EnsureItemShadow(newItem);

            Transform newTransform = newItem.transform;
            Vector3 targetScale = newTransform.localScale;
            newTransform.localScale = targetScale * 0.5f;
            newTransform
                .DOScale(targetScale, 0.2f)
                .SetEase(Ease.OutBack);

            RefreshLayerVisibility(false);

            return newItem;
        }

        public bool TryResolveFrontLayerMatch()
        {
            if (_isResolvingMatch)
            {
                return false;
            }

            if (TryGetMatchableFrontLayerItems(_matchingItems))
            {
                PlayMatchAnimation(_matchingItems);
                return true;
            }

            return false;
        }

        public List<ShelfItemView> ReserveFrontLayerItems(int maxCount)
        {
            List<ShelfItemView> reserved = new List<ShelfItemView>();

            if (maxCount <= 0 || _isResolvingMatch)
            {
                return reserved;
            }

            if (TryGetFrontLayerItems(_frontLayerItems) == false || _frontLayerItems.Count == 0)
            {
                return reserved;
            }

            for (int i = 0; i < _frontLayerItems.Count && reserved.Count < maxCount; i++)
            {
                ShelfItemView item = _frontLayerItems[i];

                if (item != null && item.ItemId != 0 && item.IsRemoving == false)
                {
                    item.MarkRemoving();
                    SetItemCollidersEnabled(item, false);
                    reserved.Add(item);
                }
            }

            return reserved;
        }

        public List<ShelfItemView> ReserveFrontLayerItemsOfType(int itemId, int maxCount)
        {
            List<ShelfItemView> reserved = new List<ShelfItemView>();

            if (maxCount <= 0 || itemId == 0 || _isResolvingMatch)
            {
                return reserved;
            }

            if (TryGetFrontLayerItems(_frontLayerItems) == false || _frontLayerItems.Count == 0)
            {
                return reserved;
            }

            for (int i = 0; i < _frontLayerItems.Count && reserved.Count < maxCount; i++)
            {
                ShelfItemView item = _frontLayerItems[i];

                if (item != null && item.ItemId == itemId && item.IsRemoving == false)
                {
                    item.MarkRemoving();
                    SetItemCollidersEnabled(item, false);
                    reserved.Add(item);
                }
            }

            return reserved;
        }

        public void BreakReservedItems(List<ShelfItemView> items)
        {
            if (items == null || items.Count == 0 || _isResolvingMatch)
            {
                return;
            }

            PlayMatchAnimation(items);
        }

        public bool TryGetMatchableFrontLayerItems(List<ShelfItemView> matchableItems)
        {
            if (matchableItems == null)
            {
                throw new ArgumentNullException(nameof(matchableItems));
            }

            matchableItems.Clear();

            if (_isResolvingMatch || TryGetFrontLayer(out ShelfLayerView frontLayer) == false)
            {
                return false;
            }

            CollectFrontLayerItems(frontLayer, _frontLayerItems);

            for (int itemIndex = 0; itemIndex < _frontLayerItems.Count; itemIndex++)
            {
                ShelfItemView candidateItem = _frontLayerItems[itemIndex];

                if (candidateItem.ItemId == 0 || candidateItem.IsRemoving)
                {
                    continue;
                }

                matchableItems.Add(candidateItem);

                for (int nextItemIndex = itemIndex + 1; nextItemIndex < _frontLayerItems.Count; nextItemIndex++)
                {
                    ShelfItemView nextItem = _frontLayerItems[nextItemIndex];

                    if (nextItem.ItemId != candidateItem.ItemId || nextItem.IsRemoving)
                    {
                        continue;
                    }

                    matchableItems.Add(nextItem);

                    if (matchableItems.Count == MatchItemCount)
                    {
                        return true;
                    }
                }

                matchableItems.Clear();
            }

            return false;
        }

        public bool TryGetFrontLayerItems(List<ShelfItemView> frontItems)
        {
            if (frontItems == null)
            {
                throw new ArgumentNullException(nameof(frontItems));
            }

            frontItems.Clear();

            if (_isResolvingMatch || TryGetFrontLayer(out ShelfLayerView frontLayer) == false)
            {
                return false;
            }

            CollectFrontLayerItems(frontLayer, frontItems);
            return true;
        }

        public bool HasFreeFrontSlot()
        {
            return TryGetFreeFrontSlot(
                out _,
                out _);
        }

        private bool TryGetFreeFrontSlot(
            out ShelfLayerView frontLayer,
            out Transform freeSlot)
        {
            frontLayer = null;
            freeSlot = null;

            if (TryGetFrontLayer(out frontLayer) == false)
                return false;

            foreach (Transform slot in frontLayer.Slots)
            {
                if (slot != null && slot.childCount == 0)
                {
                    freeSlot = slot;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetNearestFreeFrontSlot(
            Vector3 worldPoint,
            out ShelfLayerView frontLayer,
            out Transform freeSlot)
        {
            frontLayer = null;
            freeSlot = null;

            if (TryGetFrontLayer(out frontLayer) == false)
                return false;

            float bestDistance = float.MaxValue;

            foreach (Transform slot in frontLayer.Slots)
            {
                if (slot == null || slot.childCount > 0)
                    continue;

                float distance = Vector2.SqrMagnitude(slot.position - worldPoint);

                if (distance >= bestDistance)
                    continue;

                freeSlot = slot;
                bestDistance = distance;
            }

            return freeSlot != null;
        }

        private bool TryGetFrontLayer(out ShelfLayerView frontLayer)
        {
            frontLayer = null;

            if (EnsureFrontLayer() == false)
                return false;

            frontLayer = _layersRoot.GetChild(0).GetComponent<ShelfLayerView>();

            return frontLayer != null;
        }

        private void CollectFrontLayerItems(
            ShelfLayerView frontLayer,
            List<ShelfItemView> frontItems)
        {
            frontItems.Clear();

            foreach (Transform slot in frontLayer.Slots)
            {
                if (slot == null)
                    continue;

                ShelfItemView item = slot.GetComponentInChildren<ShelfItemView>(true);

                if (item != null && item.gameObject.activeInHierarchy)
                {
                    frontItems.Add(item);
                }
            }
        }

        private void PlayMatchAnimation(List<ShelfItemView> items)
        {
            _isResolvingMatch = true;
            int removedItemCount = items.Count;
            Sequence sequence = DOTween.Sequence();
            float shrinkStartTime = _matchPulseDuration;

            foreach (ShelfItemView item in items)
            {
                item.MarkRemoving();
                DOTween.Kill(item.transform);
                SetItemCollidersEnabled(
                    item,
                    false);

                Transform itemTransform = item.transform;
                Vector3 startScale = itemTransform.localScale;

                sequence.Insert(
                    0f,
                    itemTransform
                        .DOScale(startScale * _matchPulseScale, _matchPulseDuration)
                        .SetEase(Ease.OutSine));

                sequence.Insert(
                    shrinkStartTime,
                    itemTransform
                        .DOScale(Vector3.zero, _matchShrinkDuration)
                        .SetEase(Ease.InSine));

                SpriteRenderer[] renderers = itemTransform.GetComponentsInChildren<SpriteRenderer>(true);

                foreach (SpriteRenderer spriteRenderer in renderers)
                {
                    if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                        continue;

                    SpriteRenderer targetRenderer = spriteRenderer;
                    DOTween.Kill(targetRenderer);

                    sequence.Insert(
                        shrinkStartTime,
                        DOTween.To(
                                () => targetRenderer.color.a,
                                alpha =>
                                {
                                    Color color = targetRenderer.color;
                                    color.a = alpha;
                                    targetRenderer.color = color;
                                },
                                0f,
                                _matchShrinkDuration)
                            .SetEase(Ease.InSine));
                }
            }

            sequence.OnComplete(() =>
            {
                foreach (ShelfItemView item in items)
                {
                    if (item == null)
                        continue;

                    item.transform.SetParent(null);

                    if (Application.isPlaying)
                        Destroy(item.gameObject);
                    else
                        DestroyImmediate(item.gameObject);
                }

                _isResolvingMatch = false;
                RemoveEmptyFrontLayers();
                RefreshLayerVisibility(true);
                OnItemsRemoved(removedItemCount);
                Matched?.Invoke(transform.position, removedItemCount);
                TryResolveFrontLayerMatch();
            });
        }

        private void OnItemsRemoved(int itemCount)
        {
            ItemsRemoved?.Invoke(itemCount);
        }

        private void SetItemCollidersEnabled(
            ShelfItemView item,
            bool isEnabled)
        {
            Collider2D[] colliders = item.GetComponentsInChildren<Collider2D>(true);

            foreach (Collider2D itemCollider in colliders)
                itemCollider.enabled = isEnabled;
        }

        private bool EnsureFrontLayer()
        {
            if (_layersRoot == null)
                return false;

            if (_layersRoot.childCount > 0)
                return true;

            if (_layerPrefab == null)
                return false;

            ShelfLayerView layer = Instantiate(_layerPrefab, _layersRoot);
            layer.transform.localPosition = Vector3.zero;
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = Vector3.one;
            layer.gameObject.SetActive(true);

            return true;
        }

        private bool HasItems(ShelfLayerView layer)
        {
            foreach (Transform slot in layer.Slots)
            {
                if (slot != null && slot.childCount > 0)
                    return true;
            }

            return false;
        }

        private void BindItemsInLayer(
            ShelfLayerView layer,
            int layerIndex)
        {
            foreach (Transform slot in layer.Slots)
            {
                if (slot == null)
                    continue;

                ShelfItemView item = slot.GetComponentInChildren<ShelfItemView>(true);

                if (item != null)
                {
                    _spawner.EnsureItemShadow(item);
                    _spawner.EnsureItemSelectionVisual(item);
                    item.Bind(this, layer, slot, layerIndex);
                }
            }
        }

        private void ClearLayers()
        {
            if (_layersRoot == null)
                return;

            for (int i = _layersRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _layersRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

    }
}
