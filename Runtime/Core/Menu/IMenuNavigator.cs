using System;

namespace _Project.Core.Menu
{
    public interface IMenuNavigator
    {
        event Action<MenuTab> TabChanged;
        event Action<MenuPopup> PopupOpened;
        event Action PopupClosed;

        MenuTab CurrentTab { get; }

        void GoToTab(MenuTab tab);
        void OpenPopup(MenuPopup popup);
        void ClosePopup();
    }
}
