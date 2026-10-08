using _Project.Core.Audio;
using _Project.Core.Leaderboard;
using _Project.Core.Menu;
using _Project.Core.Shop;
using _Project.Features.Analytics;
using _Project.Infrastructure.Menu;
using _Project.Infrastructure.Prime;
using _Project.UI.Common;
using _Project.UI.Menu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private AudioAsset _menuMusic;
        [SerializeField] private ShopConfig _shopConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<MenuNavigator>(Lifetime.Scoped).As<IMenuNavigator>();
            builder.RegisterEntryPoint<MenuPresenter>();
            builder.RegisterEntryPoint<MenuMusicPresenter>().WithParameter(_menuMusic);

            builder.RegisterInstance(_shopConfig);
            builder.RegisterEntryPoint<PrimeShopService>(Lifetime.Singleton).As<IShopService>();
            builder.RegisterEntryPoint<ShopAnalyticsReporter>(Lifetime.Singleton);
            builder.Register<MenuShopEntry>(Lifetime.Scoped).As<IShopEntry>();
            builder.RegisterComponentInHierarchy<OutOfLivesPopup>();
        }
    }
}
