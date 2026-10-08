using System;
using _Project.Core.Shop;
using _Project.Core.StateMachine;

namespace _Project.UI.Building
{
    public class BuildingShopEntry : IShopEntry
    {
        private readonly IPendingShopRequest _pending;
        private readonly IGameLauncher _launcher;

        public BuildingShopEntry(IPendingShopRequest pending, IGameLauncher launcher)
        {
            _pending = pending ?? throw new ArgumentNullException(nameof(pending));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        }

        public void OpenShop()
        {
            _pending.Request();
            _launcher.GoToMenu();
        }
    }
}
