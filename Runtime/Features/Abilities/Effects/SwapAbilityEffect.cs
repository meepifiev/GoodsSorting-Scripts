using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class SwapAbilityEffect : IAbilityEffect
    {
        private const int MinItemsToSwap = 3;

        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly ISwapFx _swapFx;
        private readonly IAbilityRuntime _abilityRuntime;

        private readonly List<ShelfItemView> _items = new List<ShelfItemView>();
        private readonly List<SlotTarget> _targets = new List<SlotTarget>();
        private readonly List<ShelfCellView> _cells = new List<ShelfCellView>();
        private readonly List<SwapMove> _moves = new List<SwapMove>();

        public SwapAbilityEffect(
            LevelShelfSpawner levelShelfSpawner,
            ISwapFx swapFx,
            IAbilityRuntime abilityRuntime)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _swapFx = swapFx ?? throw new ArgumentNullException(nameof(swapFx));
            _abilityRuntime = abilityRuntime ?? throw new ArgumentNullException(nameof(abilityRuntime));
        }

        public AbilityType Type => AbilityType.Swap;

        public bool Apply()
        {
            if (_abilityRuntime.IsBusy)
            {
                return false;
            }

            _items.Clear();
            _targets.Clear();
            _cells.Clear();
            _moves.Clear();

            foreach (ShelfCellView cell in _levelShelfSpawner.SpawnedCells)
            {
                if (cell == null || cell.IsResolvingMatch)
                {
                    continue;
                }

                ShelfLayerView frontLayer = cell.GetFrontLayer();

                if (frontLayer == null)
                {
                    continue;
                }

                _cells.Add(cell);

                foreach (Transform slot in frontLayer.Slots)
                {
                    if (slot == null)
                    {
                        continue;
                    }

                    _targets.Add(new SlotTarget(cell, frontLayer, slot));

                    ShelfItemView item = slot.GetComponentInChildren<ShelfItemView>(true);

                    if (item != null && item.ItemId != 0 && item.IsRemoving == false && item.gameObject.activeInHierarchy)
                    {
                        _items.Add(item);
                    }
                }
            }

            if (_items.Count < MinItemsToSwap || _targets.Count < _items.Count)
            {
                return false;
            }

            _items.Sort((left, right) => left.ItemId.CompareTo(right.ItemId));

            for (int i = 0; i < _items.Count; i++)
            {
                SlotTarget target = _targets[i];
                _moves.Add(new SwapMove(_items[i], target.Cell, target.Layer, target.Slot));
            }

            Vector3 center = ComputeCenter();
            List<SwapMove> moves = new List<SwapMove>(_moves);
            List<ShelfCellView> cells = new List<ShelfCellView>(_cells);

            _abilityRuntime.SetBusy(true);
            _swapFx.Play(center, moves, () => Resolve(cells));

            return true;
        }

        private void Resolve(List<ShelfCellView> cells)
        {
            foreach (ShelfCellView cell in cells)
            {
                if (cell != null)
                {
                    cell.RefreshLayerVisibility(true);
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

        private Vector3 ComputeCenter()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (ShelfCellView cell in _cells)
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

        private readonly struct SlotTarget
        {
            public SlotTarget(ShelfCellView cell, ShelfLayerView layer, Transform slot)
            {
                Cell = cell;
                Layer = layer;
                Slot = slot;
            }

            public ShelfCellView Cell { get; }
            public ShelfLayerView Layer { get; }
            public Transform Slot { get; }
        }
    }
}
