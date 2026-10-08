using UnityEngine;

namespace _Project.Features.Building
{
    [RequireComponent(typeof(ParticleSystem))]
    public class BuildingItemAppearFx : MonoBehaviour
    {
        private ParticleSystem _particles;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
        }

        public void Play(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            _particles.Play(true);
        }
    }
}
