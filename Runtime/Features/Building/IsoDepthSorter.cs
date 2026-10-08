using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Features.Building
{
    public class IsoDepthSorter
    {
        private const float FlatHeightTolerance = 0.15f;
        private const float Epsilon = 0.0001f;

        private readonly IsoProjection _projection;

        private readonly List<List<int>> _inFrontOf = new List<List<int>>();
        private readonly List<int> _order = new List<int>();
        private readonly List<byte> _visitState = new List<byte>();

        public IsoDepthSorter(IsoProjection projection)
        {
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public IReadOnlyList<int> SortBackToFront(IReadOnlyList<IsoBox> boxes)
        {
            if (boxes == null)
            {
                throw new ArgumentNullException(nameof(boxes));
            }

            int count = boxes.Count;
            PrepareBuffers(count);

            Rect[] screenBounds = new Rect[count];

            for (int i = 0; i < count; i++)
            {
                screenBounds[i] = _projection.ScreenBounds(boxes[i]);
            }

            for (int a = 0; a < count; a++)
            {
                for (int b = a + 1; b < count; b++)
                {
                    if (screenBounds[a].Overlaps(screenBounds[b]) == false)
                    {
                        continue;
                    }

                    if (IsInFront(boxes[a], boxes[b]))
                    {
                        _inFrontOf[a].Add(b);
                    }
                    else
                    {
                        _inFrontOf[b].Add(a);
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                Visit(i);
            }

            return _order;
        }

        private void PrepareBuffers(int count)
        {
            _order.Clear();
            _visitState.Clear();

            while (_inFrontOf.Count < count)
            {
                _inFrontOf.Add(new List<int>());
            }

            for (int i = 0; i < count; i++)
            {
                _inFrontOf[i].Clear();
                _visitState.Add(0);
            }
        }

        private void Visit(int index)
        {
            if (_visitState[index] != 0)
            {
                return;
            }

            _visitState[index] = 1;

            List<int> behind = _inFrontOf[index];

            for (int i = 0; i < behind.Count; i++)
            {
                Visit(behind[i]);
            }

            _visitState[index] = 2;
            _order.Add(index);
        }

        private bool IsInFront(IsoBox a, IsoBox b)
        {
            Vector3 aMin = a.Min;
            Vector3 aMax = a.Max;
            Vector3 bMin = b.Min;
            Vector3 bMax = b.Max;

            float overlapX = Mathf.Min(aMax.x, bMax.x) - Mathf.Max(aMin.x, bMin.x);
            float overlapY = Mathf.Min(aMax.y, bMax.y) - Mathf.Max(aMin.y, bMin.y);
            float overlapZ = Mathf.Min(aMax.z, bMax.z) - Mathf.Max(aMin.z, bMin.z);

            if (overlapX <= Epsilon)
            {
                return aMax.x < bMax.x;
            }

            if (overlapY <= Epsilon)
            {
                return aMax.y < bMax.y;
            }

            if (aMin.z >= bMax.z - FlatHeightTolerance)
            {
                return true;
            }

            if (bMin.z >= aMax.z - FlatHeightTolerance)
            {
                return false;
            }

            if (overlapX <= overlapY && overlapX <= overlapZ && Mathf.Abs(aMin.x - bMin.x) > Epsilon)
            {
                return aMin.x < bMin.x;
            }

            if (overlapY <= overlapZ && Mathf.Abs(aMin.y - bMin.y) > Epsilon)
            {
                return aMin.y < bMin.y;
            }

            if (Mathf.Abs(aMin.z - bMin.z) > Epsilon)
            {
                return aMin.z > bMin.z;
            }

            Vector3 aCenter = a.Center;
            Vector3 bCenter = b.Center;
            float aDepth = aCenter.x + aCenter.y - aCenter.z;
            float bDepth = bCenter.x + bCenter.y - bCenter.z;
            return aDepth < bDepth;
        }
    }
}
