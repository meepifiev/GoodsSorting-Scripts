using System;
using System.Collections;
using Spine.Unity;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public class HammerFxView : MonoBehaviour, IHammerFx
    {
        [SerializeField] private SkeletonAnimation _fxPrefab;
        [SerializeField] private string _animationName = "animation";
        [SerializeField] private float _impactDelay = 0.6f;
        [SerializeField] private float _lifetime = 1.2f;

        public void PlayAt(Vector3 worldPosition, Action onImpact)
        {
            if (_fxPrefab == null)
            {
                onImpact?.Invoke();
                return;
            }

            SkeletonAnimation fx = Instantiate(_fxPrefab, worldPosition, Quaternion.identity);
            fx.AnimationState.SetAnimation(0, _animationName, false);
            Destroy(fx.gameObject, _lifetime);

            StartCoroutine(ImpactRoutine(onImpact));
        }

        private IEnumerator ImpactRoutine(Action onImpact)
        {
            yield return new WaitForSecondsRealtime(_impactDelay);
            onImpact?.Invoke();
        }
    }
}
