using System;
using _Project.Core.Building;
using _Project.Core.Economy;
using _Project.Core.Level;
using _Project.Core.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class WinGemsRewardView : MonoBehaviour
    {
        private const string AmountKey = "win.gems";

        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _amountLabel;
        [SerializeField] private GameObject _bar;
        [SerializeField] private TextMeshProUGUI _barAmountLabel;
        [SerializeField] private WinRewardPresenter _presenter;
        [SerializeField] private CoinFlyEffect _fly;
        [SerializeField] private RectTransform _flySource;
        [SerializeField] private RectTransform _flyTarget;

        private IBuildingAccess _access;
        private ILevelService _levelService;
        private ILocalizationService _localization;
        private IWalletStorage _wallet;
        private int _pending;

        [Inject]
        public void Construct(IBuildingAccess access, ILevelService levelService, ILocalizationService localization, IWalletStorage wallet)
        {
            _access = access ?? throw new ArgumentNullException(nameof(access));
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        private void OnEnable()
        {
            if (_access == null)
            {
                return;
            }

            _wallet.Changed += OnWalletChanged;
            _presenter.Claimed += OnClaimed;
            Redraw();
        }

        private void OnDisable()
        {
            if (_access == null)
            {
                return;
            }

            _wallet.Changed -= OnWalletChanged;
            _presenter.Claimed -= OnClaimed;
        }

        private void Start()
        {
            Redraw();
        }

        public void Initialize(
            GameObject root,
            TextMeshProUGUI amountLabel,
            GameObject bar,
            TextMeshProUGUI barAmountLabel,
            WinRewardPresenter presenter,
            CoinFlyEffect fly,
            RectTransform flySource,
            RectTransform flyTarget)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _amountLabel = amountLabel ?? throw new ArgumentNullException(nameof(amountLabel));
            _bar = bar ?? throw new ArgumentNullException(nameof(bar));
            _barAmountLabel = barAmountLabel ?? throw new ArgumentNullException(nameof(barAmountLabel));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _fly = fly ?? throw new ArgumentNullException(nameof(fly));
            _flySource = flySource ?? throw new ArgumentNullException(nameof(flySource));
            _flyTarget = flyTarget ?? throw new ArgumentNullException(nameof(flyTarget));
        }

        private void Redraw()
        {
            bool granted = _access.GrantsGemsForLevel(_levelService.CurrentLevel);
            _pending = granted ? _access.GemsForLevel(_levelService.CurrentLevel) : 0;
            _root.SetActive(granted);
            _bar.SetActive(granted);

            if (granted == false)
            {
                return;
            }

            _amountLabel.text = string.Format(_localization.Get(AmountKey), _access.GemsForLevel(_levelService.CurrentLevel));
            RedrawBar();
        }

        private void RedrawBar()
        {
            _barAmountLabel.text = Mathf.Max(0, _wallet.Get(ResourceType.Gems) - _pending).ToString();
        }

        private void OnWalletChanged(ResourceType resource)
        {
            if (resource == ResourceType.Gems)
            {
                RedrawBar();
            }
        }

        private void OnClaimed(RectTransform source)
        {
            if (_pending == 0)
            {
                return;
            }

            _pending = 0;
            RedrawBar();
            _fly.Play(_flySource, _flyTarget);
        }
    }
}
