using _Project.Core.Audio;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Common
{
    [RequireComponent(typeof(Button))]
    public class ClickSoundButton : MonoBehaviour
    {
        [SerializeField] private AudioAsset _clickSound;

        private IAudioService _audioService;
        private Button _button;

        [Inject]
        public void Construct(IAudioService audioService)
        {
            _audioService = audioService;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Play);
        }

        private void Play()
        {
            _audioService.PlayOneShotSafe(_clickSound);
        }
    }
}
