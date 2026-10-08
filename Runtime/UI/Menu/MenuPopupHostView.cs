using System;
using _Project.Core.Menu;
using UnityEngine;
using VContainer;

namespace _Project.UI.Menu
{
    public class MenuPopupHostView : MonoBehaviour
    {
        [SerializeField] private MenuPopupPanel[] _panels;

        private IMenuNavigator _navigator;

        [Inject]
        public void Construct(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            _navigator.PopupOpened += OnPopupOpened;
            _navigator.PopupClosed += OnPopupClosed;

            foreach (MenuPopupPanel panel in _panels)
            {
                panel.Closed += OnPanelClosed;
                panel.Hide();
            }
        }

        private void OnDisable()
        {
            _navigator.PopupOpened -= OnPopupOpened;
            _navigator.PopupClosed -= OnPopupClosed;

            foreach (MenuPopupPanel panel in _panels)
            {
                panel.Closed -= OnPanelClosed;
            }
        }

        private void HideAll()
        {
            foreach (MenuPopupPanel panel in _panels)
            {
                panel.Hide();
            }
        }

        private void OnPopupOpened(MenuPopup popup)
        {
            foreach (MenuPopupPanel panel in _panels)
            {
                if (panel.Popup == popup)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
        }

        private void OnPopupClosed()
        {
            HideAll();
        }

        private void OnPanelClosed()
        {
            _navigator.ClosePopup();
        }
    }
}
