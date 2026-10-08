using System;
using _Project.Core.Time;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay
{
    public class LevelTimerView : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private CanvasGroup _lastSecondsWarningOverlay;
        [SerializeField] private float _oneMinuteWarningScale = 1.2f;
        [SerializeField] private float _oneMinuteWarningDuration = 0.18f;
        [SerializeField] private float _lastSecondsWarningScale = 1.06f;
        [SerializeField] private float _lastSecondsWarningDuration = 0.18f;
        [SerializeField] private float _lastSecondsOverlayAlpha = 0.6f;
        [SerializeField] private float _lastSecondsOverlayFadeDuration = 0.28f;
        [SerializeField] private float _lastSecondsOverlayFadeOutDuration = 0.65f;

        private ILevelTimer _levelTimer;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        private void OnDisable()
        {
            ResetAnimation();
        }

        private void OnDestroy()
        {
            if (_levelTimer != null)
            {
                _levelTimer.RemainingTimeChanged -= UpdateTimerText;
                _levelTimer.OneMinuteRemaining -= PlayOneMinuteWarning;
                _levelTimer.LastSecondsTicked -= PlayLastSecondsWarning;
            }

            DOTween.Kill(transform);
            DOTween.Kill(_lastSecondsWarningOverlay);
        }

        [Inject]
        private void Construct(ILevelTimer levelTimer)
        {
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
            _levelTimer.RemainingTimeChanged += UpdateTimerText;
            _levelTimer.OneMinuteRemaining += PlayOneMinuteWarning;
            _levelTimer.LastSecondsTicked += PlayLastSecondsWarning;
            UpdateTimerText(_levelTimer.RemainingSeconds);
        }

        private void UpdateTimerText(float remainingSeconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            int minutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds % SecondsPerMinute;
            _timerLabel.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void PlayOneMinuteWarning()
        {
            PlayScaleAnimation(_oneMinuteWarningScale, _oneMinuteWarningDuration);
        }

        private void PlayLastSecondsWarning()
        {
            PlayScaleAnimation(_lastSecondsWarningScale, _lastSecondsWarningDuration);
            PlayLastSecondsOverlayAnimation();
        }

        private void PlayLastSecondsOverlayAnimation()
        {
            if (_lastSecondsWarningOverlay == null)
            {
                return;
            }

            DOTween.Kill(_lastSecondsWarningOverlay);
            _lastSecondsWarningOverlay.alpha = 0f;

            DOTween
                .To(
                    () => _lastSecondsWarningOverlay.alpha,
                    value => _lastSecondsWarningOverlay.alpha = value,
                    _lastSecondsOverlayAlpha,
                    _lastSecondsOverlayFadeDuration)
                .SetEase(Ease.OutSine)
                .SetTarget(_lastSecondsWarningOverlay)
                .OnComplete(
                    () =>
                    {
                        DOTween
                            .To(
                                () => _lastSecondsWarningOverlay.alpha,
                                value => _lastSecondsWarningOverlay.alpha = value,
                                0f,
                                _lastSecondsOverlayFadeOutDuration)
                            .SetEase(Ease.InSine)
                            .SetTarget(_lastSecondsWarningOverlay);
                    });
        }

        private void PlayScaleAnimation(
            float scaleMultiplier,
            float duration)
        {
            DOTween.Kill(transform);

            transform
                .DOScale(_initialScale * scaleMultiplier, duration)
                .SetEase(Ease.OutSine)
                .SetTarget(transform)
                .OnComplete(
                    () =>
                    {
                        transform
                            .DOScale(_initialScale, duration)
                            .SetEase(Ease.InSine)
                            .SetTarget(transform);
                    });
        }

        private void ResetAnimation()
        {
            DOTween.Kill(transform);
            transform.localScale = _initialScale;

            if (_lastSecondsWarningOverlay == null)
            {
                return;
            }

            DOTween.Kill(_lastSecondsWarningOverlay);
            _lastSecondsWarningOverlay.alpha = 0f;
        }
    }
}
