using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class CrashAbilityEffect : IAbilityEffect
    {
        private const int BreakCount = 3;

        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly IHammerFx _hammerFx;
        private readonly IAbilityRuntime _abilityRuntime;
        private readonly List<ShelfItemView> _buffer = new List<ShelfItemView>();
        private readonly Dictionary<int, int> _typeCounts = new Dictionary<int, int>();

        public CrashAbilityEffect(LevelShelfSpawner levelShelfSpawner, IHammerFx hammerFx, IAbilityRuntime abilityRuntime)
        {
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _hammerFx = hammerFx ?? throw new ArgumentNullException(nameof(hammerFx));
            _abilityRuntime = abilityRuntime ?? throw new ArgumentNullException(nameof(abilityRuntime));
        }

        public AbilityType Type => AbilityType.Crash;

        public bool Apply()
        {
            int targetItemId = FindMostAbundantType();

            if (targetItemId == 0)
            {
                return false;
            }

            List<ShelfCellView> cells = new List<ShelfCellView>();
            List<List<ShelfItemView>> reservedPerCell = new List<List<ShelfItemView>>();
            int remaining = BreakCount;
            Vector3 sum = Vector3.zero;
            int cellCount = 0;

            foreach (ShelfCellView cell in _levelShelfSpawner.SpawnedCells)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (cell == null)
                {
                    continue;
                }

                List<ShelfItemView> reserved = cell.ReserveFrontLayerItemsOfType(targetItemId, remaining);

                if (reserved.Count == 0)
                {
                    continue;
                }

                cells.Add(cell);
                reservedPerCell.Add(reserved);
                remaining -= reserved.Count;
                sum += cell.transform.position;
                cellCount++;
            }

            if (cellCount == 0)
            {
                return false;
            }

            Vector3 center = sum / cellCount;
            List<ShelfCellView> targetCells = cells;
            List<List<ShelfItemView>> targetReserved = reservedPerCell;

            _abilityRuntime.SetBusy(true);
            _hammerFx.PlayAt(center, () =>
            {
                for (int i = 0; i < targetCells.Count; i++)
                {
                    targetCells[i].BreakReservedItems(targetReserved[i]);
                }

                _abilityRuntime.SetBusy(false);
            });

            return true;
        }

        private int FindMostAbundantType()
        {
            _typeCounts.Clear();

            foreach (ShelfCellView cell in _levelShelfSpawner.SpawnedCells)
            {
                if (cell == null || cell.IsResolvingMatch)
                {
                    continue;
                }

                if (cell.TryGetFrontLayerItems(_buffer) == false)
                {
                    continue;
                }

                foreach (ShelfItemView item in _buffer)
                {
                    if (item == null || item.ItemId == 0 || item.IsRemoving)
                    {
                        continue;
                    }

                    _typeCounts[item.ItemId] = _typeCounts.TryGetValue(item.ItemId, out int count) ? count + 1 : 1;
                }
            }

            int bestId = 0;
            int bestCount = 0;

            foreach (KeyValuePair<int, int> pair in _typeCounts)
            {
                if (pair.Value >= BreakCount && pair.Value > bestCount)
                {
                    bestId = pair.Key;
                    bestCount = pair.Value;
                }
            }

            return bestId;
        }
    }
}
