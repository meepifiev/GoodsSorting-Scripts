using System;
using _Project.Core.Lives;
using _Project.Core.Menu;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuShopShortcut : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private bool _skipWhenLivesInfinite;

        private IMenuNavigator _navigator;
        private ILivesService _lives;

        [Inject]
        public void Construct(IMenuNavigator navigator, ILivesService lives)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            if (_skipWhenLivesInfinite && _lives.IsInfinite)
            {
                return;
            }

            _navigator.GoToTab(MenuTab.Shop);
        }
    }
}
