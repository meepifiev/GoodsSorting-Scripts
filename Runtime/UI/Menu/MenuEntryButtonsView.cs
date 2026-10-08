using System;
using _Project.Core.Menu;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuEntryButtonsView : MonoBehaviour
    {
        [SerializeField] private MenuEntryButton[] _buttons;

        private IMenuNavigator _navigator;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            foreach (MenuEntryButton button in _buttons)
            {
                if (button != null)
                {
                    button.Clicked += OnEntryClicked;
                }
            }
        }

        private void OnDisable()
        {
            foreach (MenuEntryButton button in _buttons)
            {
                if (button != null)
                {
                    button.Clicked -= OnEntryClicked;
                }
            }
        }

        private void OnEntryClicked(MenuPopup popup)
        {
            _navigator.OpenPopup(popup);
        }
    }
}
