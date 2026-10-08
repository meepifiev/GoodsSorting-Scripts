using System;
using _Project.Core.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Building
{
    public class BuildingHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gemsLabel;
        [SerializeField] private Button _exitButton;

        private IWalletStorage _wallet;

        public event Action ExitClicked;

        [Inject]
        public void Construct(IWalletStorage wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        private void OnEnable()
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void Start()
        {
            _wallet.Changed += OnWalletChanged;
            Redraw();
        }

        private void OnDestroy()
        {
            if (_wallet != null)
            {
                _wallet.Changed -= OnWalletChanged;
            }
        }

        public void Initialize(TextMeshProUGUI gemsLabel, Button exitButton)
        {
            _gemsLabel = gemsLabel ?? throw new ArgumentNullException(nameof(gemsLabel));
            _exitButton = exitButton ?? throw new ArgumentNullException(nameof(exitButton));
        }

        private void Redraw()
        {
            _gemsLabel.text = _wallet.Get(ResourceType.Gems).ToString();
        }

        private void OnWalletChanged(ResourceType resource)
        {
            if (resource == ResourceType.Gems)
            {
                Redraw();
            }
        }

        private void OnExitClicked()
        {
            ExitClicked?.Invoke();
        }
    }
}
