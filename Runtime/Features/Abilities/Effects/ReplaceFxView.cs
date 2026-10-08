using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Features.CellSpawner;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class ReplaceFxView : SpineCastFxView, IReplaceFx
    {
        [SerializeField] private float _startDelay = 0.15f;
        [SerializeField] private float _perItemStagger = 0.06f;

        public void Play(Vector3 center, IReadOnlyList<ShelfItemView> items, Action<ShelfItemView> convert, Action onComplete)
        {
            PlaySpine(center);
            StartCoroutine(Run(items, convert, onComplete));
        }

        private IEnumerator Run(IReadOnlyList<ShelfItemView> items, Action<ShelfItemView> convert, Action onComplete)
        {
            if (_startDelay > 0f)
            {
                yield return new WaitForSeconds(_startDelay);
            }

            for (int i = 0; i < items.Count; i++)
            {
                ShelfItemView item = items[i];

                if (item != null)
                {
                    SpawnBursts(item.transform.position);
                    convert?.Invoke(item);
                }

                if (_perItemStagger > 0f)
                {
                    yield return new WaitForSeconds(_perItemStagger);
                }
            }

            onComplete?.Invoke();
        }
    }
}
