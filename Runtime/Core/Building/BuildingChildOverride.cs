using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingChildOverride
    {
        private const float Epsilon = 0.0001f;

        [SerializeField] private int[] _path = Array.Empty<int>();
        [SerializeField] private bool _hasTransform;
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localEulerAngles;
        [SerializeField] private Vector3 _localScale = Vector3.one;
        [SerializeField] private bool _hasActive;
        [SerializeField] private bool _active = true;
        [SerializeField] private bool _hasRenderer;
        [SerializeField] private bool _rendererEnabled = true;
        [SerializeField] private int _sortingOrder;

        public BuildingChildOverride(int[] path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public IReadOnlyList<int> Path => _path ?? Array.Empty<int>();

        public bool HasTransform => _hasTransform;
        public Vector3 LocalPosition => _localPosition;
        public Vector3 LocalEulerAngles => _localEulerAngles;
        public Vector3 LocalScale => _localScale;

        public bool HasActive => _hasActive;
        public bool Active => _active;

        public bool HasRenderer => _hasRenderer;
        public bool RendererEnabled => _rendererEnabled;
        public int SortingOrder => _sortingOrder;

        public bool IsEmpty => _hasTransform == false && _hasActive == false && _hasRenderer == false;

        public void SetTransform(Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            _hasTransform = true;
            _localPosition = localPosition;
            _localEulerAngles = localEulerAngles;
            _localScale = localScale;
        }

        public void SetActive(bool active)
        {
            _hasActive = true;
            _active = active;
        }

        public void SetRenderer(bool enabled, int sortingOrder)
        {
            _hasRenderer = true;
            _rendererEnabled = enabled;
            _sortingOrder = sortingOrder;
        }

        public bool Matches(BuildingChildOverride other)
        {
            if (other == null || SamePath(other) == false)
            {
                return false;
            }

            if (_hasTransform != other._hasTransform || _hasActive != other._hasActive || _hasRenderer != other._hasRenderer)
            {
                return false;
            }

            if (_hasTransform
                && ((_localPosition - other._localPosition).sqrMagnitude > Epsilon
                    || (_localEulerAngles - other._localEulerAngles).sqrMagnitude > Epsilon
                    || (_localScale - other._localScale).sqrMagnitude > Epsilon))
            {
                return false;
            }

            if (_hasActive && _active != other._active)
            {
                return false;
            }

            return _hasRenderer == false || (_rendererEnabled == other._rendererEnabled && _sortingOrder == other._sortingOrder);
        }

        private bool SamePath(BuildingChildOverride other)
        {
            IReadOnlyList<int> mine = Path;
            IReadOnlyList<int> theirs = other.Path;

            if (mine.Count != theirs.Count)
            {
                return false;
            }

            for (int i = 0; i < mine.Count; i++)
            {
                if (mine[i] != theirs[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
