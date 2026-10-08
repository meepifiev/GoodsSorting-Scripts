using System;
using System.Collections.Generic;

namespace _Project.Features.Building
{
    public class IsoLayout
    {
        private const float StartDepth = 1f;
        private const float DepthStep = 0.05f;
        private const float GroundThickness = 0.2f;
        private const float GroundLevel = 0.15f;

        private readonly IsoDepthSorter _sorter;
        private readonly List<int> _layers = new List<int>();
        private readonly List<int> _groundIndices = new List<int>();
        private readonly List<int> _standingIndices = new List<int>();
        private readonly List<IsoBox> _groupBoxes = new List<IsoBox>();

        public IsoLayout(IsoDepthSorter sorter)
        {
            _sorter = sorter ?? throw new ArgumentNullException(nameof(sorter));
        }

        public float[] ComputeDepths(IReadOnlyList<IsoBox> boxes, IReadOnlyList<int> sortingLayers)
        {
            if (boxes == null)
            {
                throw new ArgumentNullException(nameof(boxes));
            }

            if (sortingLayers == null)
            {
                throw new ArgumentNullException(nameof(sortingLayers));
            }

            if (boxes.Count != sortingLayers.Count)
            {
                throw new ArgumentException(nameof(sortingLayers));
            }

            int count = boxes.Count;
            float[] depths = new float[count];
            CollectDistinctLayers(sortingLayers);
            int globalRank = 0;

            foreach (int layer in _layers)
            {
                SplitLayer(boxes, sortingLayers, layer);
                globalRank = RankGroup(boxes, _groundIndices, depths, count, globalRank);
                globalRank = RankGroup(boxes, _standingIndices, depths, count, globalRank);
            }

            return depths;
        }

        private void SplitLayer(IReadOnlyList<IsoBox> boxes, IReadOnlyList<int> sortingLayers, int layer)
        {
            _groundIndices.Clear();
            _standingIndices.Clear();

            for (int i = 0; i < boxes.Count; i++)
            {
                if (sortingLayers[i] != layer)
                {
                    continue;
                }

                if (IsGround(boxes[i]))
                {
                    _groundIndices.Add(i);
                }
                else
                {
                    _standingIndices.Add(i);
                }
            }
        }

        private int RankGroup(IReadOnlyList<IsoBox> boxes, List<int> indices, float[] depths, int count, int globalRank)
        {
            if (indices.Count == 0)
            {
                return globalRank;
            }

            _groupBoxes.Clear();

            foreach (int index in indices)
            {
                _groupBoxes.Add(boxes[index]);
            }

            IReadOnlyList<int> order = _sorter.SortBackToFront(_groupBoxes);

            for (int rank = 0; rank < order.Count; rank++)
            {
                int index = indices[order[rank]];
                depths[index] = StartDepth + DepthStep * (count - 1 - globalRank);
                globalRank++;
            }

            return globalRank;
        }

        private bool IsGround(IsoBox box)
        {
            return box.Size.z <= GroundThickness && box.Min.z <= GroundLevel;
        }

        private void CollectDistinctLayers(IReadOnlyList<int> sortingLayers)
        {
            _layers.Clear();

            foreach (int layer in sortingLayers)
            {
                if (_layers.Contains(layer) == false)
                {
                    _layers.Add(layer);
                }
            }

            _layers.Sort();
        }
    }
}
