using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class ReplaceAbilityEffect : IAbilityEffect
    {
        private const int MatchItemCount = 3;

        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly IReplaceFx _replaceFx;
        private readonly IAbilityRuntime _abilityRuntime;

        private readonly List<ShelfItemView> _buffer = new List<ShelfItemView>();
        private readonly List<CellFront> _board = new List<CellFront>();
        private readonly HashSet<int> _itemIds = new HashSet<int>();

        public ReplaceAbilityEffect(
            LevelShelfSpawner levelShelfSpawner,
            IReplaceFx replaceFx,
            IAbilityRuntime abilityRuntime)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _replaceFx = replaceFx ?? throw new ArgumentNullException(nameof(replaceFx));
            _abilityRuntime = abilityRuntime ?? throw new ArgumentNullException(nameof(abilityRuntime));
        }

        public AbilityType Type => AbilityType.Replace;

        public bool Apply()
        {
            if (_abilityRuntime.IsBusy)
            {
                return false;
            }

            BuildBoardSnapshot();

            if (_itemIds.Count < 2)
            {
                return false;
            }

            int fromItemId = 0;
            int toItemId = 0;
            int bestTriples = 0;
            int bestConversions = 0;

            foreach (int candidateTo in _itemIds)
            {
                foreach (int candidateFrom in _itemIds)
                {
                    if (candidateFrom == candidateTo)
                    {
                        continue;
                    }

                    int triples = 0;
                    int conversions = 0;

                    foreach (CellFront cellFront in _board)
                    {
                        int fromCount = cellFront.CountOf(candidateFrom);

                        if (fromCount == 0)
                        {
                            continue;
                        }

                        int toCount = cellFront.CountOf(candidateTo);
                        conversions += fromCount;
                        triples += (toCount + fromCount) / MatchItemCount - toCount / MatchItemCount;
                    }

                    if (triples > bestTriples || (triples == bestTriples && conversions > bestConversions))
                    {
                        bestTriples = triples;
                        bestConversions = conversions;
                        fromItemId = candidateFrom;
                        toItemId = candidateTo;
                    }
                }
            }

            if (bestTriples < 1)
            {
                return false;
            }

            List<ShelfItemView> fromItems = new List<ShelfItemView>();
            List<ShelfCellView> cells = new List<ShelfCellView>();

            foreach (CellFront cellFront in _board)
            {
                bool hasFrom = false;

                foreach (ShelfItemView item in cellFront.Items)
                {
                    if (item != null && item.ItemId == fromItemId && item.IsRemoving == false)
                    {
                        fromItems.Add(item);
                        hasFrom = true;
                    }
                }

                if (hasFrom)
                {
                    cells.Add(cellFront.Cell);
                }
            }

            int usableCount = fromItems.Count / MatchItemCount * MatchItemCount;

            if (usableCount == 0)
            {
                return false;
            }

            if (usableCount < fromItems.Count)
            {
                fromItems.RemoveRange(usableCount, fromItems.Count - usableCount);
            }

            Vector3 center = ComputeCenter(cells);
            int targetId = toItemId;

            _abilityRuntime.SetBusy(true);
            _replaceFx.Play(
                center,
                fromItems,
                item => _levelShelfSpawner.ReplaceFrontItem(item, targetId),
                () => Resolve(cells));

            return true;
        }

        private void Resolve(List<ShelfCellView> cells)
        {
            foreach (ShelfCellView cell in cells)
            {
                if (cell != null)
                {
                    cell.RefreshLayerVisibility(false);
                }
            }

            foreach (ShelfCellView cell in cells)
            {
                if (cell != null)
                {
                    cell.TryResolveFrontLayerMatch();
                }
            }

            _abilityRuntime.SetBusy(false);
        }

        private static Vector3 ComputeCenter(List<ShelfCellView> cells)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (ShelfCellView cell in cells)
            {
                if (cell == null)
                {
                    continue;
                }

                sum += cell.transform.position;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        private void BuildBoardSnapshot()
        {
            _board.Clear();
            _itemIds.Clear();

            foreach (ShelfCellView cell in _levelShelfSpawner.SpawnedCells)
            {
                if (cell == null || cell.IsResolvingMatch)
                {
                    continue;
                }

                if (cell.TryGetFrontLayerItems(_buffer) == false || _buffer.Count == 0)
                {
                    continue;
                }

                CellFront cellFront = new CellFront(cell);

                foreach (ShelfItemView item in _buffer)
                {
                    if (item == null || item.ItemId == 0 || item.IsRemoving)
                    {
                        continue;
                    }

                    cellFront.Add(item);
                    _itemIds.Add(item.ItemId);
                }

                if (cellFront.Items.Count > 0)
                {
                    _board.Add(cellFront);
                }
            }
        }

        private class CellFront
        {
            private readonly Dictionary<int, int> _counts = new Dictionary<int, int>();

            public CellFront(ShelfCellView cell)
            {
                Cell = cell;
                Items = new List<ShelfItemView>();
            }

            public ShelfCellView Cell { get; }
            public List<ShelfItemView> Items { get; }

            public void Add(ShelfItemView item)
            {
                Items.Add(item);
                _counts[item.ItemId] = _counts.TryGetValue(item.ItemId, out int count) ? count + 1 : 1;
            }

            public int CountOf(int itemId)
            {
                return _counts.TryGetValue(itemId, out int count) ? count : 0;
            }
        }
    }
}
