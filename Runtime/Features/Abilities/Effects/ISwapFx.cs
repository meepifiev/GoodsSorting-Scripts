using System;
using System.Collections.Generic;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public readonly struct SwapMove
    {
        public SwapMove(ShelfItemView item, ShelfCellView cell, ShelfLayerView layer, Transform slot)
        {
            Item = item;
            Cell = cell;
            Layer = layer;
            Slot = slot;
        }

        public ShelfItemView Item { get; }
        public ShelfCellView Cell { get; }
        public ShelfLayerView Layer { get; }
        public Transform Slot { get; }
    }

    public interface ISwapFx
    {
        void Play(Vector3 center, IReadOnlyList<SwapMove> moves, Action onComplete);
    }
}
