using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingItemPlacement
    {
        [SerializeField] private int _itemId;
        [SerializeField] private bool _flipped;
        [SerializeField] private Vector3 _isoPosition;
        [SerializeField] private int _sortingLayer;
        [SerializeField] private BuildingChildOverride[] _childOverrides = Array.Empty<BuildingChildOverride>();

        public BuildingItemPlacement(int itemId, bool flipped, Vector3 isoPosition)
        {
            _itemId = itemId;
            _flipped = flipped;
            _isoPosition = isoPosition;
        }

        public int ItemId => _itemId;
        public bool Flipped => _flipped;
        public Vector3 IsoPosition => _isoPosition;
        public int SortingLayer => _sortingLayer;

        public IReadOnlyList<BuildingChildOverride> ChildOverrides => _childOverrides ?? Array.Empty<BuildingChildOverride>();

        public void SetLayout(Vector3 isoPosition, bool flipped, int sortingLayer)
        {
            _isoPosition = isoPosition;
            _flipped = flipped;
            _sortingLayer = sortingLayer;
        }

        public void SetChildOverrides(BuildingChildOverride[] overrides)
        {
            _childOverrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
        }
    }
}
