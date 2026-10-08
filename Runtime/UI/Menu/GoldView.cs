using System;
using _Project.Core.Economy;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class GoldView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _amountLabel;

        private IWalletStorage _wallet;

        [Inject]
        public void Construct(IWalletStorage wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        private void OnEnable()
        {
            _wallet.Changed += OnWalletChanged;
            Redraw();
        }

        private void OnDisable()
        {
            _wallet.Changed -= OnWalletChanged;
        }

        private void Redraw()
        {
            _amountLabel.text = _wallet.Get(ResourceType.Gold).ToString();
        }

        private void OnWalletChanged(ResourceType resource)
        {
            if (resource == ResourceType.Gold)
            {
                Redraw();
            }
        }
    }
}
