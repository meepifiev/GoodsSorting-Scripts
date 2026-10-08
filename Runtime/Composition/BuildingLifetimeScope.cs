using _Project.Core.Building;
using _Project.Core.Shop;
using _Project.Features.Building;
using _Project.Infrastructure.Prime;
using _Project.UI.Building;
using _Project.UI.Common;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class BuildingLifetimeScope : LifetimeScope
    {
        [SerializeField] private BuildingWorldConfig _areas;
        [SerializeField] private BuildingBalanceConfig _balance;
        [SerializeField] private BuildingItemCatalog _catalog;
        [SerializeField] private BuildingWorldView _world;
        [SerializeField] private BuildingItemAppearFx _appearFx;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_areas);
            builder.RegisterInstance(_balance);
            builder.RegisterInstance(_catalog);
            builder.RegisterComponent(_world);
            builder.RegisterComponent(_appearFx);
            builder.RegisterComponentInHierarchy<BuildingCameraController>();

            builder.Register<IsoProjection>(Lifetime.Scoped);
            builder.Register<IsoDepthSorter>(Lifetime.Scoped);
            builder.Register<IsoLayout>(Lifetime.Scoped);
            builder.Register<BuildingAreaBounds>(Lifetime.Scoped);
            builder.Register<BuildingProgressStorage>(Lifetime.Scoped).As<IBuildingProgressStorage>();
            builder.Register<BuildingService>(Lifetime.Scoped).As<IBuildingService>();
            builder.Register<BuildingItemProvider>(Lifetime.Scoped);
            builder.Register<BuildingEnvironmentProvider>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<BuildingHudView>();
            builder.RegisterComponentInHierarchy<BuildingThemePopup>();
            builder.RegisterComponentInHierarchy<BuildingTaskMarkersView>();
            builder.RegisterComponentInHierarchy<BuildingNewDayBanner>();
            builder.Register<BuildingShopEntry>(Lifetime.Scoped).As<IShopEntry>();
            builder.RegisterComponentInHierarchy<OutOfLivesPopup>();

            builder.Register<BuildingItemsPresenter>(Lifetime.Scoped);
            builder.Register<BuildingFlowPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BuildingContentBootstrapper>(Lifetime.Scoped);
        }
    }
}
