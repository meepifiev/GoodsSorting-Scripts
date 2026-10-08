using UnityEngine;

namespace _Project.UI.Common
{
    public class UISpinner : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 24f;

        private void Update()
        {
            transform.Rotate(0f, 0f, _degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
