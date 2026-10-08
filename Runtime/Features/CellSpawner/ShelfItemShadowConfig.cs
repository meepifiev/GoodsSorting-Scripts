using UnityEngine;

namespace _Project.Features.CellSpawner
{
    [CreateAssetMenu(fileName = "ShelfItemShadowConfig", menuName = "_Project/Shelf Item Shadow Config", order = 3)]
    public class ShelfItemShadowConfig : ScriptableObject
    {
        [SerializeField] private Vector3 _offset = new Vector3(0.25f, 0.08f, 0f);
        [SerializeField] private Color _color = new Color(0f, 0f, 0f, 0.13f);
        [SerializeField] private int _sortingOrderOffset = 1;
        [SerializeField] private float _fadeDuration = 0.16f;

        public Vector3 Offset => _offset;
        public Color Color => _color;
        public int SortingOrderOffset => _sortingOrderOffset;
        public float FadeDuration => _fadeDuration;
    }
}
