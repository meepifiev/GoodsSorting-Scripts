using System;
using UnityEngine;

namespace _Project.Features.Building
{
    public class BuildingItemView : MonoBehaviour
    {
        [SerializeField] private int _itemId;
        [SerializeField] private Vector3 _isoSize;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private GameObject[] _themes = Array.Empty<GameObject>();

        private bool _flipped;
        private int _theme;

        public int ItemId => _itemId;
        public Vector3 IsoSize => _isoSize;
        public int ThemeCount => _themes.Length;
        public bool IsFlipped => _flipped;
        public int Theme => _theme;
        public Transform VisualRoot => _visualRoot;

        public Vector3 FlippedIsoSize => _flipped
            ? new Vector3(_isoSize.y, _isoSize.x, _isoSize.z)
            : _isoSize;

        public void Initialize(int itemId, Vector3 isoSize, Transform visualRoot, GameObject[] themes)
        {
            _itemId = itemId;
            _isoSize = isoSize;
            _visualRoot = visualRoot ?? throw new ArgumentNullException(nameof(visualRoot));
            _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        }

        public void SetFlipped(bool flipped)
        {
            _flipped = flipped;

            if (_visualRoot == null)
            {
                return;
            }

            Vector3 scale = _visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * (flipped ? -1f : 1f);
            _visualRoot.localScale = scale;
        }

        public void SetTheme(int theme)
        {
            if (_themes.Length == 0)
            {
                return;
            }

            _theme = Mathf.Clamp(theme, 0, _themes.Length - 1);

            for (int i = 0; i < _themes.Length; i++)
            {
                if (_themes[i] != null)
                {
                    _themes[i].SetActive(i == _theme);
                }
            }
        }

        public void SetScreenPosition(Vector2 screenPosition, float depth)
        {
            transform.localPosition = new Vector3(screenPosition.x, screenPosition.y, depth);
        }

        public Renderer[] GetActiveRenderers()
        {
            return GetComponentsInChildren<Renderer>(false);
        }

        public bool IsThemeRoot(GameObject candidate)
        {
            for (int i = 0; i < _themes.Length; i++)
            {
                if (_themes[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        public Sprite GetThemeSprite(int theme)
        {
            if (theme < 0 || theme >= _themes.Length || _themes[theme] == null)
            {
                return null;
            }

            SpriteRenderer renderer = _themes[theme].GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }
    }
}
