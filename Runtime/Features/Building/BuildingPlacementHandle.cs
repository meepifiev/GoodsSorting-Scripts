using UnityEngine;

namespace _Project.Features.Building
{
    public class BuildingPlacementHandle : MonoBehaviour
    {
        [SerializeField] private int _area;
        [SerializeField] private BuildingPlacementGroup _group;
        [SerializeField] private int _day;
        [SerializeField] private int _task;
        [SerializeField] private int _index;
        [SerializeField] private float _isoHeight;
        [SerializeField] private bool _flipped;
        [SerializeField] private int _sortingLayer;

        public int Area => _area;
        public BuildingPlacementGroup Group => _group;
        public int Day => _day;
        public int Task => _task;
        public int Index => _index;
        public float IsoHeight => _isoHeight;
        public bool Flipped => _flipped;
        public int SortingLayer => _sortingLayer;

        public void SetIndex(int index)
        {
            _index = index;
        }

        public void Initialize(
            int area,
            BuildingPlacementGroup group,
            int day,
            int task,
            int index,
            float isoHeight,
            bool flipped,
            int sortingLayer)
        {
            _area = area;
            _group = group;
            _day = day;
            _task = task;
            _index = index;
            _isoHeight = isoHeight;
            _flipped = flipped;
            _sortingLayer = sortingLayer;
        }
    }
}
