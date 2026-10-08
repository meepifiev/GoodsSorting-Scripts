using System;
using _Project.Core.Combo;
using _Project.Core.Localization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class ComboView : MonoBehaviour
    {
        private const string ComboKey = "combo.counter";
        private const float FadeDuration = 0.25f;

        [SerializeField] private Image _fillImage;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private ParticleSystem _progressFx;

        private IComboService _comboService;
        private ILocalizationService _localization;

        [Inject]
        private void Construct(IComboService comboService, ILocalizationService localization)
        {
            _comboService = comboService ?? throw new ArgumentNullException(nameof(comboService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            if (_comboService == null)
            {
                return;
            }

            _comboService.Bumped += OnBumped;
            _comboService.ProgressChanged += OnProgressChanged;
            _comboService.Ended += OnEnded;
        }

        private void OnDisable()
        {
            if (_comboService == null)
            {
                return;
            }

            _comboService.Bumped -= OnBumped;
            _comboService.ProgressChanged -= OnProgressChanged;
            _comboService.Ended -= OnEnded;
        }

        private void OnDestroy()
        {
            if (_canvasGroup != null)
            {
                DOTween.Kill(_canvasGroup);
            }
        }

        private void OnBumped(int combo)
        {
            if (_label != null)
            {
                _label.text = string.Format(_localization.Get(ComboKey), combo);
            }

            Show();
            OnProgressChanged();

            if (_progressFx != null)
            {
                _progressFx.Play(true);
            }
        }

        private void OnProgressChanged()
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = _comboService.Progress;
            }
        }

        private void OnEnded()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            DOTween.Kill(_canvasGroup);

            DOTween
                .To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, 0f, FadeDuration)
                .SetEase(Ease.InSine)
                .SetTarget(_canvasGroup);
        }

        private void Show()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            DOTween.Kill(_canvasGroup);
            _canvasGroup.alpha = 1f;
        }
    }
}
