using System;
using _Project.Core.Economy;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class PiggyBankView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _amountText;

        private IWalletStorage _wallet;

        [Inject]
        private void Construct(IWalletStorage wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        private void OnEnable()
        {
            if (_wallet == null)
            {
                return;
            }

            _wallet.Changed += OnWalletChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_wallet != null)
            {
                _wallet.Changed -= OnWalletChanged;
            }
        }

        private void OnWalletChanged(ResourceType resource)
        {
            if (resource == ResourceType.Gold)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_amountText != null)
            {
                _amountText.text = _wallet.Get(ResourceType.Gold).ToString();
            }
        }
    }
}
