using _Project.Core.Advertising;
using _Project.Core.Audio;
using _Project.Core.Economy;
using _Project.Core.Level;
using _Project.Core.Localization;
using _Project.Core.StateMachine;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class LosePanelPresenter : MonoBehaviour
    {
        private const string TimeUpKey = "lose.time_up";
        private const string OutOfSpaceKey = "lose.out_of_space";
        private const string OfferTimeKey = "lose.offer.time";
        private const string OfferSpaceKey = "lose.offer.space";

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [Tooltip("Optional offer label under the title. Set by lose reason: add time vs free space.")]
        [SerializeField] private TextMeshProUGUI _offerText;
        [Tooltip("Optional offer icon next to the label. Swapped by lose reason.")]
        [SerializeField] private Image _offerIcon;
        [SerializeField] private Sprite _offerTimeSprite;
        [SerializeField] private Sprite _offerSpaceSprite;

        [Header("Staged reveal")]
        [SerializeField] private CanvasGroup _offerGroup;
        [SerializeField] private float _revealDelay = 0.3f;
        [SerializeField] private float _revealDuration = 0.3f;
        [SerializeField] private float _revealFromScale = 0.85f;

        [Header("Buttons")]
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _goldButton;
        [SerializeField] private Button _freeButton;

        [Header("Revive")]
        [SerializeField] private int _goldCost = 800;
        [SerializeField] private float _reviveSeconds = 30f;
        [SerializeField] private string _adRewardId = "revive";

        private ILevelService _level;
        private IWalletStorage _wallet;
        private IAdvertisingService _advertising;
        private IAudioService _audio;
        private IGameLauncher _launcher;
        private GameplayAudioConfig _audioConfig;
        private ILocalizationService _localization;

        private bool _resolved;
        private Sequence _revealSequence;

        [Inject]
        public void Construct(
            ILevelService level,
            IWalletStorage wallet,
            IAdvertisingService advertising,
            IAudioService audio,
            IGameLauncher launcher,
            GameplayAudioConfig audioConfig,
            ILocalizationService localization)
        {
            _level = level;
            _wallet = wallet;
            _advertising = advertising;
            _audio = audio;
            _launcher = launcher;
            _audioConfig = audioConfig;
            _localization = localization;
        }

        private void OnEnable()
        {
            _resolved = false;

            bool outOfSpace = _level != null && _level.LoseReason == LevelLostReason.OutOfSpace;

            if (_titleText != null && _level != null && _localization != null)
            {
                _titleText.text = _localization.Get(outOfSpace ? OutOfSpaceKey : TimeUpKey);
            }

            if (_offerText != null && _localization != null)
            {
                _offerText.text = _localization.Get(outOfSpace ? OfferSpaceKey : OfferTimeKey);
            }

            if (_offerIcon != null)
            {
                Sprite offerSprite = outOfSpace ? _offerSpaceSprite : _offerTimeSprite;

                if (offerSprite != null)
                {
                    _offerIcon.sprite = offerSprite;
                }
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClicked);
            }

            if (_goldButton != null)
            {
                _goldButton.onClick.AddListener(OnGoldClicked);
            }

            if (_freeButton != null)
            {
                _freeButton.onClick.AddListener(OnFreeClicked);
            }

            PlaySound(_audioConfig != null ? _audioConfig.LevelLost : null);
            PlayStagedReveal();
        }

        private void OnDisable()
        {
            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }

            if (_goldButton != null)
            {
                _goldButton.onClick.RemoveListener(OnGoldClicked);
            }

            if (_freeButton != null)
            {
                _freeButton.onClick.RemoveListener(OnFreeClicked);
            }

            _revealSequence?.Kill();
            _revealSequence = null;
        }

        private void PlayStagedReveal()
        {
            if (_offerGroup == null)
            {
                return;
            }

            _revealSequence?.Kill();

            _offerGroup.alpha = 0f;
            _offerGroup.interactable = false;
            _offerGroup.blocksRaycasts = false;
            _offerGroup.transform.localScale = Vector3.one * _revealFromScale;

            _revealSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _revealSequence.AppendInterval(_revealDelay);
            _revealSequence.AppendCallback(EnableOfferInteraction);
            _revealSequence.Append(
                DOTween.To(() => _offerGroup.alpha, value => _offerGroup.alpha = value, 1f, _revealDuration)
                    .SetUpdate(true));
            _revealSequence.Join(
                _offerGroup.transform
                    .DOScale(1f, _revealDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
        }

        private void EnableOfferInteraction()
        {
            if (_offerGroup == null)
            {
                return;
            }

            _offerGroup.interactable = true;
            _offerGroup.blocksRaycasts = true;
        }

        private void OnExitClicked()
        {
            PlayClick();
            _launcher?.GoToMenu();
        }

        private void OnGoldClicked()
        {
            PlayClick();

            if (_resolved || _wallet == null)
            {
                return;
            }

            if (_wallet.TrySpend(ResourceType.Gold, _goldCost) == false)
            {
                return;
            }

            Revive();
        }

        private void OnFreeClicked()
        {
            PlayClick();

            if (_resolved)
            {
                return;
            }

            if (_advertising == null)
            {
                Revive();
                return;
            }

            _advertising.ShowRewarded(_adRewardId, rewarded: Revive, closed: null);
        }

        private void Revive()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_level.LoseReason == LevelLostReason.OutOfSpace)
            {
                _level.ReviveWithSpace();
            }
            else
            {
                _level.Revive(_reviveSeconds);
            }

            gameObject.SetActive(false);
        }

        private void PlayClick()
        {
            PlaySound(_audioConfig != null ? _audioConfig.UiClick : null);
        }

        private void PlaySound(AudioAsset asset)
        {
            _audio.PlayOneShotSafe(asset);
        }
    }
}
