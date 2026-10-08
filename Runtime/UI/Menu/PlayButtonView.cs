using System;
using _Project.Core.Lives;
using _Project.Core.Progress;
using _Project.Core.StateMachine;
using _Project.UI.Common;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Menu
{
    public class PlayButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ILivesService _lives;
        private IProgressStorage _progress;
        private IGameLauncher _gameLauncher;
        private OutOfLivesPopup _outOfLivesPopup;

        private bool _isLoading;

        [Inject]
        public void Construct(
            ILivesService lives,
            IProgressStorage progress,
            IGameLauncher gameLauncher,
            OutOfLivesPopup outOfLivesPopup)
        {
            _gameLauncher = gameLauncher ?? throw new ArgumentNullException(nameof(gameLauncher));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _outOfLivesPopup = outOfLivesPopup ?? throw new ArgumentNullException(nameof(outOfLivesPopup));
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnPlayClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            if (_isLoading)
            {
                return;
            }

            if (_lives.TrySpendLife() == false)
            {
                _outOfLivesPopup.Show(OnLivesGranted);
                return;
            }

            StartLoading();
        }

        private void OnLivesGranted()
        {
            if (_isLoading)
            {
                return;
            }

            if (_lives.TrySpendLife() == false)
            {
                return;
            }

            StartLoading();
        }

        private void StartLoading()
        {
            _isLoading = true;
            _button.interactable = false;

            _gameLauncher.StartGame();
        }
    }
}
