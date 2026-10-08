using System;
using _Project.Core.Level;
using _Project.Core.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class FinishLevelView : MonoBehaviour
    {
        [SerializeField] private WinWindow _winWindow;
        [SerializeField] private LoseWindow _loseWindow;
        [SerializeField] private CongratsWindow _congratsWindow;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _restartButton;

        private ILevelService _levelService;
        private IModalGate _modalGate;
        private bool _gateOpen;

        [Inject]
        private void Construct(ILevelService levelService, IModalGate modalGate)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _modalGate = modalGate ?? throw new ArgumentNullException(nameof(modalGate));
            _levelService.Finished += OnLevelFinished;
            _levelService.Resumed += OnLevelResumed;
        }

        private void OnEnable()
        {
            HideWindows();

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_levelService != null && _levelService.State != LevelState.Playing)
            {
                ShowFor(_levelService.State);
            }
        }

        private void OnDisable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            LowerGate();
        }

        private void OnDestroy()
        {
            if (_levelService != null)
            {
                _levelService.Finished -= OnLevelFinished;
                _levelService.Resumed -= OnLevelResumed;
            }

            LowerGate();
        }

        private void RaiseGate()
        {
            if (_gateOpen || _modalGate == null)
            {
                return;
            }

            _gateOpen = true;
            _modalGate.Open();
        }

        private void LowerGate()
        {
            if (_gateOpen == false || _modalGate == null)
            {
                return;
            }

            _gateOpen = false;
            _modalGate.Close();
        }

        private void OnLevelFinished(LevelFinishResult result)
        {
            ShowFor(result == LevelFinishResult.Won ? LevelState.Won : LevelState.Lost);
        }

        private void OnLevelResumed()
        {
            HideWindows();
            LowerGate();
        }

        private void ShowFor(LevelState state)
        {
            RaiseGate();
            HideWindows();

            if (state == LevelState.Won)
            {
                if (_congratsWindow != null)
                {
                    _congratsWindow.Show(ShowWinReward);
                }
                else
                {
                    ShowWinReward();
                }
            }
            else if (state == LevelState.Lost && _loseWindow != null)
            {
                _loseWindow.Show();
            }
        }

        private void ShowWinReward()
        {
            if (_congratsWindow != null)
            {
                _congratsWindow.Hide();
            }

            if (_winWindow != null)
            {
                _winWindow.Show();
            }
        }

        private void HideWindows()
        {
            if (_congratsWindow != null)
            {
                _congratsWindow.Hide();
            }

            if (_winWindow != null)
            {
                _winWindow.Hide();
            }

            if (_loseWindow != null)
            {
                _loseWindow.Hide();
            }
        }

        private void OnNextClicked()
        {
            _levelService.GoNext();
        }

        private void OnRestartClicked()
        {
            _levelService.Restart();
        }
    }
}
