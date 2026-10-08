using System;
using _Project.Core.Pause;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class PauseButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IPauseService _pauseService;

        [Inject]
        private void Construct(IPauseService pauseService)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _pauseService.Toggle();
        }
    }
}
