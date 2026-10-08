using System;
using _Project.Core.Menu;

namespace _Project.Infrastructure.Menu
{
    public class MenuNavigator : IMenuNavigator
    {
        private const MenuTab DefaultTab = MenuTab.Home;

        private MenuTab _currentTab = DefaultTab;

        public event Action<MenuTab> TabChanged;
        public event Action<MenuPopup> PopupOpened;
        public event Action PopupClosed;

        public MenuTab CurrentTab => _currentTab;

        public void GoToTab(MenuTab tab)
        {
            _currentTab = tab;
            OnTabChanged(tab);
        }

        public void OpenPopup(MenuPopup popup)
        {
            OnPopupOpened(popup);
        }

        public void ClosePopup()
        {
            OnPopupClosed();
        }

        private void OnTabChanged(MenuTab tab)
        {
            TabChanged?.Invoke(tab);
        }

        private void OnPopupOpened(MenuPopup popup)
        {
            PopupOpened?.Invoke(popup);
        }

        private void OnPopupClosed()
        {
            PopupClosed?.Invoke();
        }
    }
}
