using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfLayerVisuals
    {
        private const int BackLayerIndex = 1;
        private const int FirstHiddenLayerIndex = 2;
        private const int LayerSortingOrderStep = 100;

        private readonly Dictionary<SpriteRenderer, SpriteRendererState> _defaults = new();
        private readonly Color _backTint;
        private readonly Vector3 _layerOffset;
        private readonly float _moveDuration;
        private readonly float _colorDuration;

        public ShelfLayerVisuals(Color backTint, Vector3 layerOffset, float moveDuration, float colorDuration)
        {
            _backTint = backTint;
            _layerOffset = layerOffset;
            _moveDuration = moveDuration;
            _colorDuration = colorDuration;
        }

        public void ClearDefaults()
        {
            _defaults.Clear();
        }

        public void RegisterDefaults(GameObject item)
        {
            SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                    continue;

                if (_defaults.ContainsKey(spriteRenderer))
                    continue;

                _defaults.Add(spriteRenderer, new SpriteRendererState(spriteRenderer.color, spriteRenderer.sortingOrder));
            }
        }

        public void ApplyLayer(ShelfLayerView layer, int layerIndex, bool animate)
        {
            RegisterDefaults(layer.gameObject);
            ApplyVisibility(layer, layerIndex, animate);
            ApplyPosition(layer, layerIndex, animate);
            SyncShadows(layer);
        }

        private void ApplyVisibility(ShelfLayerView layer, int layerIndex, bool animateLayerState)
        {
            int sortingOrderOffset = GetSortingOffset(layerIndex);
            bool wasActive = layer.gameObject.activeSelf;

            if (layerIndex >= FirstHiddenLayerIndex)
            {
                RestoreRenderState(layer, sortingOrderOffset);
                SetShadowsVisible(layer, false, false);
                layer.gameObject.SetActive(false);
                return;
            }

            layer.gameObject.SetActive(true);

            if (layerIndex == BackLayerIndex)
            {
                ApplyRenderState(layer, _backTint, sortingOrderOffset, animateLayerState, wasActive == false);
                SetShadowsVisible(layer, false, false);
                return;
            }

            RestoreRenderState(layer, sortingOrderOffset, animateLayerState);
            SetShadowsVisible(layer, true, animateLayerState);
        }

        private void SetShadowsVisible(ShelfLayerView layer, bool isVisible, bool animate)
        {
            ShelfItemShadowView[] shadowViews = layer.GetComponentsInChildren<ShelfItemShadowView>(true);

            foreach (ShelfItemShadowView shadowView in shadowViews)
            {
                shadowView.SetVisible(isVisible, animate);
            }
        }

        private void SyncShadows(ShelfLayerView layer)
        {
            ShelfItemShadowView[] shadowViews = layer.GetComponentsInChildren<ShelfItemShadowView>(true);

            foreach (ShelfItemShadowView shadowView in shadowViews)
            {
                shadowView.Sync();
            }
        }

        private void ApplyPosition(ShelfLayerView layer, int layerIndex, bool animateLayerMovement)
        {
            Transform layerTransform = layer.transform;
            Vector3 targetLocalPosition = _layerOffset * layerIndex;

            DOTween.Kill(layerTransform);

            if (animateLayerMovement &&
                Application.isPlaying &&
                Vector3.SqrMagnitude(layerTransform.localPosition - targetLocalPosition) > 0.0001f)
            {
                layerTransform.DOLocalMove(targetLocalPosition, _moveDuration).SetEase(Ease.OutSine);
                return;
            }

            layerTransform.localPosition = targetLocalPosition;
        }

        private void ApplyRenderState(ShelfLayerView layer, Color tint, int sortingOrderOffset, bool animateColor, bool fadeFromTransparent)
        {
            SpriteRenderer[] renderers = layer.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                    continue;

                ApplySpriteColor(spriteRenderer, tint, animateColor, fadeFromTransparent);
                ApplySortingOffset(spriteRenderer, sortingOrderOffset);
            }
        }

        private void RestoreRenderState(ShelfLayerView layer, int sortingOrderOffset)
        {
            RestoreRenderState(layer, sortingOrderOffset, false);
        }

        private void RestoreRenderState(ShelfLayerView layer, int sortingOrderOffset, bool animateColor)
        {
            SpriteRenderer[] renderers = layer.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                    continue;

                if (_defaults.TryGetValue(spriteRenderer, out SpriteRendererState defaultState) == false)
                    continue;

                ApplySpriteColor(spriteRenderer, defaultState.Color, animateColor, false);
                spriteRenderer.sortingOrder = defaultState.SortingOrder + sortingOrderOffset;
            }
        }

        private void ApplySpriteColor(SpriteRenderer spriteRenderer, Color targetColor, bool animateColor, bool fadeFromTransparent)
        {
            DOTween.Kill(spriteRenderer);

            if (fadeFromTransparent)
            {
                Color transparentColor = targetColor;
                transparentColor.a = 0f;
                spriteRenderer.color = transparentColor;
            }

            if (animateColor && Application.isPlaying)
            {
                DOTween.To(
                        () => spriteRenderer.color,
                        color => { spriteRenderer.color = color; },
                        targetColor,
                        _colorDuration)
                    .SetEase(Ease.OutSine)
                    .SetTarget(spriteRenderer);

                return;
            }

            spriteRenderer.color = targetColor;
        }

        private int GetSortingOffset(int layerIndex)
        {
            return (FirstHiddenLayerIndex - layerIndex) * LayerSortingOrderStep;
        }

        private void ApplySortingOffset(SpriteRenderer spriteRenderer, int sortingOrderOffset)
        {
            if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                return;

            if (_defaults.TryGetValue(spriteRenderer, out SpriteRendererState defaultState) == false)
                return;

            spriteRenderer.sortingOrder = defaultState.SortingOrder + sortingOrderOffset;
        }

        private readonly struct SpriteRendererState
        {
            public SpriteRendererState(Color color, int sortingOrder)
            {
                Color = color;
                SortingOrder = sortingOrder;
            }

            public Color Color { get; }
            public int SortingOrder { get; }
        }
    }
}
