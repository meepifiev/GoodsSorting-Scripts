using System;
using System.Collections.Generic;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public interface IReplaceFx
    {
        void Play(Vector3 center, IReadOnlyList<ShelfItemView> items, Action<ShelfItemView> convert, Action onComplete);
    }
}
