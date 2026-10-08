using System;
using _Project.Core.Audio;
using _Project.Core.Building;
using UnityEngine;
using VContainer;

namespace _Project.UI.Building
{
    public class BuildingAudioPresenter : MonoBehaviour
    {
        [SerializeField] private AudioAsset _buildSound;
        [SerializeField] private AudioAsset _dayCompleteSound;

        private IBuildingService _service;
        private IAudioService _audio;

        [Inject]
        public void Construct(IBuildingService service, IAudioService audio)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        private void Start()
        {
            _service.TaskBuilt += OnTaskBuilt;
            _service.DayCompleted += OnDayCompleted;
        }

        private void OnDestroy()
        {
            if (_service == null)
            {
                return;
            }

            _service.TaskBuilt -= OnTaskBuilt;
            _service.DayCompleted -= OnDayCompleted;
        }

        public void Initialize(AudioAsset buildSound, AudioAsset dayCompleteSound)
        {
            _buildSound = buildSound;
            _dayCompleteSound = dayCompleteSound;
        }

        private void Play(AudioAsset asset)
        {
            _audio.PlayOneShotSafe(asset);
        }

        private void OnTaskBuilt(BuildingTaskRef task)
        {
            Play(_buildSound);
        }

        private void OnDayCompleted(BuildingDayResult result)
        {
            Play(_dayCompleteSound);
        }
    }
}
