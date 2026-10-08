using System;
using _Project.Core.Lives;
using _Project.Core.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class LivesView : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;
        private const string FullKey = "lives.full";
        private const string InfiniteKey = "lives.infinite";

        [SerializeField] private TextMeshProUGUI _amountLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private GameObject _addIcon;

        private ILivesService _lives;
        private ILocalizationService _localization;

        [Inject]
        public void Construct(ILivesService lives, ILocalizationService localization)
        {
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void OnEnable()
        {
            _lives.Changed += OnLivesChanged;
            Redraw();
        }

        private void OnDisable()
        {
            _lives.Changed -= OnLivesChanged;
        }

        private void Update()
        {
            RedrawTimer();
        }

        private void Redraw()
        {
            bool infinite = _lives.IsInfinite;

            if (_addIcon != null)
            {
                _addIcon.SetActive(infinite == false);
            }

            if (_amountLabel == null)
            {
                return;
            }

            _amountLabel.gameObject.SetActive(infinite == false);

            if (infinite == false)
            {
                _amountLabel.text = _lives.Current.ToString();
            }
        }

        private void RedrawTimer()
        {
            if (_timerLabel == null)
            {
                return;
            }

            if (_lives.IsInfinite)
            {
                _timerLabel.text = _localization.Get(InfiniteKey);
                return;
            }

            if (_lives.Current >= _lives.Max)
            {
                _timerLabel.text = _localization.Get(FullKey);
                return;
            }

            int totalSeconds = Mathf.CeilToInt(_lives.SecondsToNextLife);
            int minutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds % SecondsPerMinute;
            _timerLabel.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void OnLivesChanged()
        {
            Redraw();
        }
    }
}
