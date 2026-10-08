using System;
using _Project.Core.Menu;
using _Project.Core.Shop;
using VContainer.Unity;

namespace _Project.UI.Menu
{
    public class MenuPresenter : IStartable
    {
        private readonly IMenuNavigator _navigator;
        private readonly IPendingShopRequest _pendingShop;

        public MenuPresenter(IMenuNavigator navigator, IPendingShopRequest pendingShop)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _pendingShop = pendingShop ?? throw new ArgumentNullException(nameof(pendingShop));
        }

        public void Start()
        {
            _navigator.GoToTab(_pendingShop.Consume() ? MenuTab.Shop : MenuTab.Home);
        }
    }
}
