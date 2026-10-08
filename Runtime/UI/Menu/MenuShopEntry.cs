using System;
using _Project.Core.Menu;
using _Project.Core.Shop;

namespace _Project.UI.Menu
{
    public class MenuShopEntry : IShopEntry
    {
        private readonly IMenuNavigator _navigator;

        public MenuShopEntry(IMenuNavigator navigator)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void OpenShop()
        {
            _navigator.GoToTab(MenuTab.Shop);
        }
    }
}
