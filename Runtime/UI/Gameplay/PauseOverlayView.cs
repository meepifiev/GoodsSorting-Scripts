using System;
using _Project.Core.Pause;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class PauseOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _resumeButton;

        private IPauseService _pauseService;

        [Inject]
        private void Construct(IPauseService pauseService)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _pauseService.Changed += OnPauseChanged;
            OnPauseChanged(_pauseService.IsPaused);
        }

        private void OnEnable()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }
        }

        private void OnDisable()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
            }
        }

        private void OnDestroy()
        {
            if (_pauseService != null)
            {
                _pauseService.Changed -= OnPauseChanged;
            }
        }

        private void OnPauseChanged(bool paused)
        {
            if (_root != null)
            {
                _root.SetActive(paused);
            }
        }

        private void OnResumeClicked()
        {
            _pauseService.Resume();
        }
    }
}
