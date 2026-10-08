using System;
using _Project.Core.Localization;
using _Project.Core.Progress;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class LevelButtonView : MonoBehaviour
    {
        private const string LevelKey = "level.button";

        [SerializeField] private TextMeshProUGUI _label;

        private IProgressStorage _progress;
        private ILocalizationService _localization;

        [Inject]
        public void Construct(IProgressStorage progress, ILocalizationService localization)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Start()
        {
            _label.text = string.Format(_localization.Get(LevelKey), _progress.Level);
        }
    }
}
