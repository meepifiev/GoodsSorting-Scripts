using System;
using _Project.Core.Advertising;
using _Project.Core.Audio;
using _Project.Core.Economy;
using _Project.Core.Level;
using _Project.Core.Localization;
using _Project.Core.Score;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class WinRewardPresenter : MonoBehaviour
    {
        private const string TitleKey = "win.title";
        private const string ClaimKey = "win.claim";
        private const string ClaimAdKey = "win.claim_ad";

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _claimText;
        [SerializeField] private TextMeshProUGUI _claimCoinsText;
        [SerializeField] private TextMeshProUGUI _watchAdsText;
        [SerializeField] private TextMeshProUGUI _watchAdsCoinsText;

        [Header("Buttons")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _watchAdsButton;

        [Header("Reward")]
        [Tooltip("Coins granted per star earned in the level. Keep it modest.")]
        [SerializeField] private int _coinsPerStar = 1;
        [Tooltip("Minimum coins for a win even with few stars.")]
        [SerializeField] private int _minReward = 5;
        [SerializeField] private string _adRewardId = "win_reward";

        [Header("Presentation")]
        [SerializeField] private MultiplierArrowView _multiplierArrow;
        [SerializeField] private CoinFlyEffect _coinFly;
        [Tooltip("Where the coins land (drag the coin plaque / gold badge here).")]
        [SerializeField] private RectTransform _coinFlyTarget;
        [SerializeField] private WinEntranceAnimator _entrance;
        [SerializeField] private AudioAsset _claimSound;

        private ILevelService _levelService;
        private IStarsCounter _stars;
        private IWalletStorage _wallet;
        private IAudioService _audioService;
        private IAdvertisingService _advertising;
        private ILocalizationService _localization;

        private bool _claimed;
        private int _reward;

        public event Action<RectTransform> Claimed;

        [Inject]
        private void Construct(ILevelService levelService, IStarsCounter stars, IWalletStorage wallet, IAudioService audioService, IAdvertisingService advertising, ILocalizationService localization)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _stars = stars ?? throw new ArgumentNullException(nameof(stars));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _audioService = audioService;
            _advertising = advertising;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void OnEnable()
        {
            _claimed = false;

            int earnedStars = _stars != null ? _stars.Stars : 0;
            _reward = Mathf.Max(_minReward, earnedStars * _coinsPerStar);

            if (_levelText != null && _levelService != null)
            {
                _levelText.text = "<color=#FF9E1B>" + string.Format(_localization.Get(TitleKey), _levelService.CurrentLevel) + "</color>";
            }

            if (_claimText != null)
            {
                _claimText.text = _localization.Get(ClaimKey);
            }

            if (_claimCoinsText != null)
            {
                _claimCoinsText.text = _reward.ToString();
            }

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(OnClaimClicked);
            }

            if (_watchAdsButton != null)
            {
                _watchAdsButton.onClick.AddListener(OnWatchAdsClicked);
            }

            if (_multiplierArrow != null)
            {
                _multiplierArrow.StartOscillation();
            }

            UpdateWatchAdsText();

            if (_entrance != null)
            {
                _entrance.Play();
            }
        }

        private void OnDisable()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(OnClaimClicked);
            }

            if (_watchAdsButton != null)
            {
                _watchAdsButton.onClick.RemoveListener(OnWatchAdsClicked);
            }
        }

        private void Update()
        {
            if (_claimed == false && _multiplierArrow != null && _multiplierArrow.IsRunning)
            {
                UpdateWatchAdsText();
            }
        }

        private int CurrentMultiplier()
        {
            return _multiplierArrow != null ? _multiplierArrow.CurrentMultiplier : 1;
        }

        private void UpdateWatchAdsText()
        {
            int multiplier = CurrentMultiplier();

            if (_watchAdsText != null)
            {
                _watchAdsText.text = string.Format(_localization.Get(ClaimAdKey), multiplier);
            }

            if (_watchAdsCoinsText != null)
            {
                _watchAdsCoinsText.text = (_reward * multiplier).ToString();
            }
        }

        private void OnClaimClicked()
        {
            ClaimAndProceed(_reward, _claimButton != null ? (RectTransform)_claimButton.transform : null);
        }

        private void OnWatchAdsClicked()
        {
            if (_claimed)
            {
                return;
            }

            int amount = _reward * CurrentMultiplier();
            RectTransform source = _watchAdsButton != null ? (RectTransform)_watchAdsButton.transform : null;

            if (_advertising == null)
            {
                ClaimAndProceed(amount, source);
                return;
            }

            _advertising.ShowRewarded(
                _adRewardId,
                rewarded: () => ClaimAndProceed(amount, source),
                closed: null);
        }

        private void ClaimAndProceed(int amount, RectTransform source)
        {
            if (_claimed)
            {
                return;
            }

            _claimed = true;
            _multiplierArrow?.Stop();
            _wallet.Add(ResourceType.Gold, amount);
            Claimed?.Invoke(source);

            _audioService.PlayOneShotSafe(_claimSound);

            if (_coinFly != null && _coinFly.Play(source, _coinFlyTarget))
            {
                DOVirtual.DelayedCall(_coinFly.TotalDuration, GoNext, false).SetLink(gameObject);
            }
            else
            {
                GoNext();
            }
        }

        private void GoNext()
        {
            _levelService.GoNext();
        }
    }
}
