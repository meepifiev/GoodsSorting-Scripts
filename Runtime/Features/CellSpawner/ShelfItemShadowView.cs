using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Features.CellSpawner
{
    [ExecuteAlways]
    public class ShelfItemShadowView : MonoBehaviour
    {
        private ShelfItemShadowConfig _config;

        private Vector3 _shadowOffset = new(0.25f, 0.08f, 0f);
        private Color _shadowColor = new(0f, 0f, 0f, 0.13f);
        private int _sortingOrderOffset = 1;
        private float _fadeDuration = 0.16f;

        private readonly List<ShadowSprite> _shadowSprites = new();

        private Transform _shadowRoot;
        private bool _isVisible = true;
        private float _visibilityAlpha = 1f;
        private Vector3 _selectionOffset;

        public static bool IsShadowRenderer(SpriteRenderer spriteRenderer)
        {
            return spriteRenderer != null &&
                   spriteRenderer.GetComponent<ShelfItemShadowRenderer>() != null;
        }

        private void Awake()
        {
            Rebuild();
        }

        private void LateUpdate()
        {
            if (_shadowRoot == null || _shadowSprites.Count == 0)
                Rebuild();

            SyncShadows();
        }

        public void SetConfig(ShelfItemShadowConfig config)
        {
            _config = config;
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            if (_config == null)
            {
                return;
            }

            _shadowOffset = _config.Offset;
            _shadowColor = _config.Color;
            _sortingOrderOffset = _config.SortingOrderOffset;
            _fadeDuration = _config.FadeDuration;
        }

        public void Rebuild()
        {
            ApplyConfig();

            DestroyShadowRoot();
            DestroyExistingShadowObjects();
            _shadowSprites.Clear();

            _shadowRoot = new GameObject("Shadow").transform;
            _shadowRoot.SetParent(transform, false);
            _shadowRoot.localPosition = _shadowOffset;
            _shadowRoot.localRotation = Quaternion.identity;
            _shadowRoot.localScale = Vector3.one;

            SpriteRenderer[] sourceRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer sourceRenderer in sourceRenderers)
            {
                if (IsShadowRenderer(sourceRenderer))
                    continue;

                GameObject shadowObject = new($"{sourceRenderer.gameObject.name}_Shadow");
                shadowObject.transform.SetParent(_shadowRoot, false);
                ApplyRelativeTransform(
                    sourceRenderer.transform,
                    shadowObject.transform);

                SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowObject.AddComponent<ShelfItemShadowRenderer>();

                _shadowSprites.Add(
                    new ShadowSprite(
                        sourceRenderer,
                        shadowRenderer));
            }

            SyncShadows();
        }

        public void EnsureBuilt()
        {
            if (_shadowRoot != null && _shadowSprites.Count > 0)
                return;

            Rebuild();
        }

        public void Sync()
        {
            EnsureBuilt();
            SyncShadows();
        }

        public void SetSelectionOffset(
            Vector3 selectionOffset,
            float duration)
        {
            EnsureBuilt();

            if (_shadowRoot != null)
                DOTween.Kill(_shadowRoot);

            if (duration <= 0f || Application.isPlaying == false)
            {
                _selectionOffset = selectionOffset;
                SyncShadows();
                return;
            }

            DOTween.To(
                    () => _selectionOffset,
                    value =>
                    {
                        _selectionOffset = value;
                        SyncShadows();
                    },
                    selectionOffset,
                    duration)
                .SetEase(Ease.OutSine)
                .SetTarget(_shadowRoot);
        }

        public void SetVisible(bool isVisible)
        {
            SetVisible(isVisible, false);
        }

        public void SetVisible(
            bool isVisible,
            bool animate)
        {
            bool shouldAnimate = isVisible &&
                                 animate &&
                                 Application.isPlaying &&
                                 (_isVisible == false || _visibilityAlpha < 0.999f);

            _isVisible = isVisible;
            DOTween.Kill(this);
            EnsureBuilt();

            if (isVisible == false)
            {
                _visibilityAlpha = 0f;
                SyncShadows();
                return;
            }

            if (shouldAnimate == false)
            {
                _visibilityAlpha = 1f;
                SyncShadows();
                return;
            }

            _visibilityAlpha = 0f;
            SyncShadows();

            DOTween.To(
                    () => _visibilityAlpha,
                    value =>
                    {
                        _visibilityAlpha = value;
                        SyncShadows();
                    },
                    1f,
                    _fadeDuration)
                .SetEase(Ease.OutSine)
                .SetTarget(this);
        }

        private void SyncShadows()
        {
            if (_shadowRoot == null)
                return;

            _shadowRoot.localPosition = _shadowOffset + _selectionOffset;

            foreach (ShadowSprite shadowSprite in _shadowSprites)
            {
                if (shadowSprite.Source == null || shadowSprite.Shadow == null)
                    continue;

                Color color = _shadowColor;
                color.a *= shadowSprite.Source.color.a;
                color.a *= _visibilityAlpha;

                shadowSprite.Source.shadowCastingMode = ShadowCastingMode.Off;
                shadowSprite.Source.receiveShadows = false;
                shadowSprite.Shadow.sprite = shadowSprite.Source.sprite;
                shadowSprite.Shadow.flipX = shadowSprite.Source.flipX;
                shadowSprite.Shadow.flipY = shadowSprite.Source.flipY;
                shadowSprite.Shadow.drawMode = shadowSprite.Source.drawMode;
                shadowSprite.Shadow.size = shadowSprite.Source.size;
                shadowSprite.Shadow.sortingLayerID = shadowSprite.Source.sortingLayerID;
                shadowSprite.Shadow.sortingOrder = shadowSprite.Source.sortingOrder - _sortingOrderOffset;
                shadowSprite.Shadow.color = color;
                shadowSprite.Shadow.enabled = (_isVisible || _visibilityAlpha > 0f) &&
                                              shadowSprite.Source.enabled;
                shadowSprite.Shadow.shadowCastingMode = ShadowCastingMode.Off;
                shadowSprite.Shadow.receiveShadows = false;
                ApplyRelativeTransform(
                    shadowSprite.Source.transform,
                    shadowSprite.Shadow.transform);
            }
        }

        private void ApplyRelativeTransform(
            Transform sourceTransform,
            Transform shadowTransform)
        {
            shadowTransform.localPosition = transform.InverseTransformPoint(sourceTransform.position);
            shadowTransform.localRotation = Quaternion.Inverse(transform.rotation) * sourceTransform.rotation;
            shadowTransform.localScale = ScaleMath.DivideScale(
                sourceTransform.lossyScale,
                transform.lossyScale);
        }

        private void DestroyShadowRoot()
        {
            if (_shadowRoot == null)
                return;

            GameObject shadowObject = _shadowRoot.gameObject;
            _shadowRoot = null;

            if (Application.isPlaying)
                Destroy(shadowObject);
            else
                DestroyImmediate(shadowObject);
        }

        private void DestroyExistingShadowObjects()
        {
            ShelfItemShadowRenderer[] shadowRenderers = GetComponentsInChildren<ShelfItemShadowRenderer>(true);

            foreach (ShelfItemShadowRenderer shadowRenderer in shadowRenderers)
            {
                DestroyShadowObject(shadowRenderer.gameObject);
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.name == "Shadow")
                    DestroyShadowObject(child.gameObject);
            }
        }

        private void DestroyShadowObject(GameObject shadowObject)
        {
            if (shadowObject == null)
                return;

            if (Application.isPlaying)
                Destroy(shadowObject);
            else
                DestroyImmediate(shadowObject);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private readonly struct ShadowSprite
        {
            public ShadowSprite(
                SpriteRenderer source,
                SpriteRenderer shadow)
            {
                Source = source;
                Shadow = shadow;
            }

            public SpriteRenderer Source { get; }
            public SpriteRenderer Shadow { get; }
        }
    }

    public class ShelfItemShadowRenderer : MonoBehaviour, IShelfShadow
    {
    }
}
