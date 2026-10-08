using System;
using _Project.Core.Building;
using _Project.Core.Localization;
using _Project.Core.StateMachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Menu
{
    public class BuildingHomeButtonView : MonoBehaviour
    {
        private const string LabelKey = "home.building";
        private const string LockedHintKey = "home.building.locked";

        [SerializeField] private Button _button;
        [SerializeField] private Image _buttonImage;
        [SerializeField] private Sprite _unlockedSprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _lockedHint;

        private IBuildingAccess _access;
        private IGameLauncher _launcher;
        private ILocalizationService _localization;
        private bool _launching;

        [Inject]
        public void Construct(IBuildingAccess access, IGameLauncher launcher, ILocalizationService localization)
        {
            _access = access ?? throw new ArgumentNullException(nameof(access));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void Start()
        {
            Refresh();
        }

        public void Initialize(
            Button button,
            Image buttonImage,
            Sprite unlockedSprite,
            Sprite lockedSprite,
            TextMeshProUGUI label,
            TextMeshProUGUI lockedHint,
            GameObject lockIcon)
        {
            _button = button ?? throw new ArgumentNullException(nameof(button));
            _buttonImage = buttonImage ?? throw new ArgumentNullException(nameof(buttonImage));
            _unlockedSprite = unlockedSprite;
            _lockedSprite = lockedSprite;
            _label = label ?? throw new ArgumentNullException(nameof(label));
            _lockedHint = lockedHint ?? throw new ArgumentNullException(nameof(lockedHint));
        }

        private void Refresh()
        {
            bool unlocked = _access.IsUnlocked;
            _button.interactable = unlocked;
            _buttonImage.sprite = unlocked ? _unlockedSprite : _lockedSprite;
            _label.text = _localization.Get(LabelKey);
            _lockedHint.text = string.Format(_localization.Get(LockedHintKey), _access.UnlockLevel);
            _lockedHint.gameObject.SetActive(unlocked == false);
        }

        private void OnClicked()
        {
            if (_launching || _access.IsUnlocked == false)
            {
                return;
            }

            _launching = true;
            _button.interactable = false;
            _launcher.GoToBuilding();
        }
    }
}
