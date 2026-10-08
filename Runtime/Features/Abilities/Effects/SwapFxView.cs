using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class SwapFxView : SpineCastFxView, ISwapFx
    {
        [SerializeField] private float _gatherDuration = 0.28f;
        [SerializeField] private float _scatterDuration = 0.30f;
        [SerializeField] private float _perItemStagger = 0.03f;

        public void Play(Vector3 center, IReadOnlyList<SwapMove> moves, Action onComplete)
        {
            PlaySpine(center);
            StartCoroutine(Run(center, moves, onComplete));
        }

        private IEnumerator Run(Vector3 center, IReadOnlyList<SwapMove> moves, Action onComplete)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                Transform itemTransform = moves[i].Item != null ? moves[i].Item.transform : null;

                if (itemTransform == null)
                {
                    continue;
                }

                DOTween.Kill(itemTransform);
                itemTransform
                    .DOMove(center, _gatherDuration)
                    .SetDelay(i * _perItemStagger)
                    .SetEase(Ease.InBack);
            }

            yield return new WaitForSeconds(_gatherDuration + moves.Count * _perItemStagger + 0.02f);

            for (int i = 0; i < moves.Count; i++)
            {
                Transform itemTransform = moves[i].Item != null ? moves[i].Item.transform : null;
                Transform slot = moves[i].Slot;

                if (itemTransform == null || slot == null)
                {
                    continue;
                }

                DOTween.Kill(itemTransform);
                itemTransform
                    .DOMove(slot.position, _scatterDuration)
                    .SetDelay(i * _perItemStagger)
                    .SetEase(Ease.OutBack);
            }

            yield return new WaitForSeconds(_scatterDuration + moves.Count * _perItemStagger + 0.02f);

            for (int i = 0; i < moves.Count; i++)
            {
                SwapMove move = moves[i];

                if (move.Item == null)
                {
                    continue;
                }

                DOTween.Kill(move.Item.transform);
                move.Item.AttachToSlot(move.Cell, move.Layer, move.Slot, 0);
            }

            onComplete?.Invoke();
        }
    }
}
