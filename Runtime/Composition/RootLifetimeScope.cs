using _Project.Composition.Startup;
using _Project.Composition.Startup.States;
using _Project.Core.Abilities;
using _Project.Core.Advertising;
using _Project.Core.Analytics;
using _Project.Core.Assets;
using _Project.Core.Audio;
using _Project.Core.Building;
using _Project.Core.Economy;
using _Project.Core.Factories;
using _Project.Core.Leaderboard;
using _Project.Core.Level;
using _Project.Core.Lives;
using _Project.Core.Localization;
using _Project.Core.Platform;
using _Project.Core.Progress;
using _Project.Core.Purchasing;
using _Project.Core.SceneCreation;
using _Project.Core.SceneManagement;
using _Project.Core.Shop;
using _Project.Core.StateMachine;
using _Project.Core.Time;
using _Project.Features.Analytics;
using _Project.Features.Building;
using _Project.Features.Cameras;
using _Project.Features.Legacy;
using _Project.Features.Legacy.Levels;
using _Project.Infrastructure.Abilities;
using _Project.Infrastructure.Assets;
using _Project.Infrastructure.Audio;
using _Project.Infrastructure.Factories;
using _Project.Infrastructure.Lives;
using _Project.Infrastructure.Menu;
using _Project.Infrastructure.Prime;
using _Project.Infrastructure.SceneManagement;
using _Project.Infrastructure.StateMachine;
using _Project.Infrastructure.Time;
using TransitionsPlus;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private LocalizationConfig _localizationConfig;
        [SerializeField] private TransitionProfile _sceneTransitionProfile;
        [SerializeField] private LeaderboardConfig _leaderboardConfig;
        [SerializeField] private BuildingBalanceConfig _buildingBalanceConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterServices(builder);
            RegisterPlatform(builder);
            RegisterPlayerState(builder);
            RegisterStateMachine(builder);
            RegisterStates(builder);
            RegisterFactories(builder);
        }

        private void RegisterFactories(IContainerBuilder builder) => 
            builder.Register<GameSceneLoadService>(Lifetime.Singleton).As<IGameSceneLoadService>();

        private void RegisterPlayerState(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LivesService>().As<ILivesService>();
            builder.Register<WalletStorage>(Lifetime.Singleton).As<IWalletStorage>();
            builder.Register<ProgressStorage>(Lifetime.Singleton).As<IProgressStorage>();
            builder.Register<FirstLaunchState>(Lifetime.Singleton).As<IFirstLaunchState>();
            builder.Register<PendingShopRequest>(Lifetime.Singleton).As<IPendingShopRequest>();
            builder.Register<BoosterInventory>(Lifetime.Singleton).As<IBoosterInventory>();
            builder.Register<WinsStorage>(Lifetime.Singleton).As<IWinsStorage>();
            builder.Register<BuildingAccess>(Lifetime.Singleton).As<IBuildingAccess>();
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(_localizationConfig);
            builder.RegisterInstance(_sceneTransitionProfile);
            builder.RegisterInstance(_leaderboardConfig);
            builder.RegisterInstance(_buildingBalanceConfig);
        }

        private void RegisterPlatform(IContainerBuilder builder)
        {
            builder.Register<PrimeSDKInitializer>(Lifetime.Singleton).As<IPlatformInitializer>();
            builder.Register<PrimeAnalyticsService>(Lifetime.Singleton).As<IAnalyticsService>();
            builder.RegisterEntryPoint<EconomyAnalyticsReporter>(Lifetime.Singleton);
            builder.Register<PrimeLocalizationService>(Lifetime.Singleton).As<ILocalizationService>();
            builder.Register<PrimePurchaseService>(Lifetime.Singleton).As<IPurchaseService>();
            builder.RegisterEntryPoint<PrimeAdvertisingService>().As<IAdvertisingService>();
            builder.Register<PrimeLeaderboardService>(Lifetime.Singleton).As<ILeaderboardService>();
            builder.Register<PrimePlayerAccountService>(Lifetime.Singleton).As<IPlayerAccountService>();
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<ObjectFactory>(Lifetime.Singleton).As<IObjectFactory>();
            builder.Register<TransitionFactory>(Lifetime.Singleton).As<ITransitionFactory>();
            builder.Register<SceneReadySignal>(Lifetime.Singleton).As<ISceneReadySignal>();
            builder.Register<LoadingCurtain>(Lifetime.Singleton).As<ILoadingCurtain>();
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
            builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
            builder.RegisterEntryPoint<SoundSettingsService>(Lifetime.Singleton).As<ISoundSettings>();
            builder.Register<TimeService>(Lifetime.Singleton).As<ITimeService>();
            builder.Register<MainCameraProvider>(Lifetime.Singleton).As<IMainCameraProvider>();
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
            builder.Register<AddressableLevelCatalog>(Lifetime.Singleton).As<ILevelCatalog>();
            builder.Register<LegacyLevelLoadingService>(Lifetime.Singleton)
                .As<ILevelLoadingService<LegacyLevelConfig>>();
        }

        private void RegisterStateMachine(IContainerBuilder builder)
        {
            builder.Register<StateFactory>(Lifetime.Singleton).As<IStateFactory>();
            builder.Register<GameStateMachine>(Lifetime.Singleton).As<IGameStateMachine>();
            builder.Register<GameLauncher>(Lifetime.Singleton).As<IGameLauncher>();
        }

        private void RegisterStates(IContainerBuilder builder)
        {
            builder.Register<BootstrapState>(Lifetime.Transient);
            builder.Register<LoadSceneState>(Lifetime.Transient);
            builder.Register<MainMenuSceneState>(Lifetime.Transient);
            builder.Register<GameSceneState>(Lifetime.Transient);
            builder.Register<BuildingSceneState>(Lifetime.Transient);
        }
    }
}
