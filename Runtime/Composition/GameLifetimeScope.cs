using _Project.Composition.Startup;
using _Project.Core.Abilities;
using _Project.Core.Audio;
using _Project.Core.Combo;
using _Project.Core.Level;
using _Project.Core.Pause;
using _Project.Core.SceneCreation;
using _Project.Core.Score;
using _Project.Core.Shop;
using _Project.Core.Time;
using _Project.Core.UI;
using _Project.Features.Abilities;
using _Project.Features.Abilities.Effects;
using _Project.Features.Analytics;
using _Project.Features.Building;
using _Project.Features.CellSpawner;
using _Project.Features.Combo;
using _Project.Features.GameSceneCreation;
using _Project.Features.Leaderboard;
using _Project.Features.Level;
using _Project.Features.Pause;
using _Project.Features.Score;
using _Project.Features.Services.InactivityHintServices;
using _Project.Features.Shop;
using _Project.Features.Timers;
using _Project.Features.UI;
using _Project.Infrastructure.Time;
using _Project.UI.Gameplay;
using _Project.UI.Gameplay.Abilities;
using _Project.UI.Gameplay.Debugging;
using _Project.UI.Gameplay.FinishLevels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelShelfSpawner _levelShelfSpawner;
        [SerializeField] private AbilityPanel _abilityPanel;
        [SerializeField] private AbilitiesConfig _abilitiesConfig;
        [SerializeField] private GameplayAudioConfig _gameplayAudioConfig;
        [SerializeField] private BoosterShopConfig _boosterShopConfig;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_levelShelfSpawner);
            builder.RegisterComponent(_abilityPanel);
            builder.RegisterInstance(_abilitiesConfig);
            builder.RegisterInstance(_gameplayAudioConfig);
            builder.RegisterInstance(_boosterShopConfig);
            builder.Register<BoosterShop>(Lifetime.Scoped).As<IBoosterShop>();
            builder.RegisterComponentInHierarchy<BoosterGetMorePopup>();
            builder.Register<GameSceneFactory>(Lifetime.Scoped).As<IGameSceneFactory>();
            builder.Register<AbilityFactory>(Lifetime.Scoped).As<IAbilityFactory>();
            builder.Register<AbilityRuntime>(Lifetime.Scoped).As<IAbilityRuntime>();
            builder.Register<FreezeAbilityEffect>(Lifetime.Scoped).As<IAbilityEffect>();
            builder.Register<CrashAbilityEffect>(Lifetime.Scoped).As<IAbilityEffect>();
            builder.Register<SwapAbilityEffect>(Lifetime.Scoped).As<IAbilityEffect>();
            builder.Register<ReplaceAbilityEffect>(Lifetime.Scoped).As<IAbilityEffect>();
            builder.Register<AbilityService>(Lifetime.Scoped).As<IAbilityService>();
            builder.RegisterEntryPoint<LevelTimer>(Lifetime.Scoped).As<ILevelTimer>();
            builder.Register<TimeFreezeService>(Lifetime.Scoped).As<ITimeFreeze>();
            builder.RegisterEntryPoint<LevelService>(Lifetime.Scoped).As<ILevelService>();
#if UNITY_EDITOR
            builder.RegisterEntryPoint<DevLevelSwitcher>(Lifetime.Scoped).AsSelf();
#endif
            builder.RegisterEntryPoint<LeaderboardWinReporter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BuildingGemsWinReporter>(Lifetime.Scoped);
            builder.Register<AbilityHintServiceStub>(Lifetime.Scoped).As<IAbilityHintService>();
            builder.RegisterEntryPoint<InactivityHintService>(Lifetime.Scoped).As<IInactivityHintService>();
            builder.RegisterEntryPoint<GameSceneBootstrapper>(Lifetime.Scoped);
            builder.RegisterEntryPoint<AbilityPanelPresenter>(Lifetime.Scoped);

            builder.Register<PauseService>(Lifetime.Scoped).As<IPauseService>();
            builder.Register<ModalGate>(Lifetime.Scoped).As<IModalGate>();
            builder.RegisterEntryPoint<StarsCounter>(Lifetime.Scoped).As<IStarsCounter>();
            builder.RegisterEntryPoint<ComboService>(Lifetime.Scoped).As<IComboService>();
            builder.RegisterEntryPoint<LevelCounterPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameplayAnalyticsReporter>(Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<GameplayDebugPanel>();
            builder.RegisterComponentInHierarchy<ShelfItemDragController>();
            builder.RegisterComponentInHierarchy<HammerFxView>().As<IHammerFx>();
            builder.RegisterComponentInHierarchy<SwapFxView>().As<ISwapFx>();
            builder.RegisterComponentInHierarchy<ReplaceFxView>().As<IReplaceFx>();
            builder.RegisterComponentInHierarchy<LevelCounterView>();
            builder.RegisterComponentInHierarchy<StarFlyEffect>();

            builder.RegisterEntryPoint<GameplayAudioPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameplayMusicPresenter>(Lifetime.Scoped);
        }
    }
}
