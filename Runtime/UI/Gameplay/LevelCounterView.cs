using System;
using _Project.Core.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class LevelCounterView : MonoBehaviour
    {
        private const string CounterKey = "level.counter";

        [SerializeField] private TextMeshProUGUI _label;

        private ILocalizationService _localization;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void SetLevel(int level)
        {
            _label.text = string.Format(_localization.Get(CounterKey), level);
        }
    }
}
