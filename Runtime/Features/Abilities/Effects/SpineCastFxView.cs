using Spine.Unity;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public abstract class SpineCastFxView : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation _spinePrefab;
        [SerializeField] private GameObject[] _burstPrefabs;
        [SerializeField] private string _startAnimation = "Start";
        [SerializeField] private string _stopAnimation = "Stop";
        [SerializeField] private float _spineScale = 2f;
        [SerializeField] private float _lifetime = 1.6f;

        protected void PlaySpine(Vector3 position)
        {
            if (_spinePrefab != null)
            {
                SkeletonAnimation fx = Instantiate(_spinePrefab, position, Quaternion.identity);
                fx.transform.localScale = Vector3.one * _spineScale;
                fx.AnimationState.SetAnimation(0, _startAnimation, false);

                if (string.IsNullOrEmpty(_stopAnimation) == false)
                {
                    fx.AnimationState.AddAnimation(0, _stopAnimation, false, 0f);
                }

                Destroy(fx.gameObject, _lifetime);
            }

            SpawnBursts(position);
        }

        protected void SpawnBursts(Vector3 position)
        {
            if (_burstPrefabs == null)
            {
                return;
            }

            foreach (GameObject burstPrefab in _burstPrefabs)
            {
                if (burstPrefab == null)
                {
                    continue;
                }

                GameObject burst = Instantiate(burstPrefab, position, Quaternion.identity);
                Destroy(burst, _lifetime);
            }
        }
    }
}
