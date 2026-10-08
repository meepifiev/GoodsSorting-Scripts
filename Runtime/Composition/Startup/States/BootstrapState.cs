using System;
using System.Threading;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.Level;
using _Project.Core.Platform;
using _Project.Core.Progress;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using _Project.Core.StateMachine.States.Interfaces;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup.States
{
    public class BootstrapState : IState
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly IPlatformInitializer _platformInitializer;
        private readonly IAnalyticsService _analytics;
        private readonly ILevelCatalog _levelCatalog;
        private readonly IFirstLaunchState _firstLaunchState;

        public BootstrapState(
            IGameStateMachine stateMachine,
            IPlatformInitializer platformInitializer,
            IAnalyticsService analytics,
            ILevelCatalog levelCatalog,
            IFirstLaunchState firstLaunchState)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _platformInitializer = platformInitializer ?? throw new ArgumentNullException(nameof(platformInitializer));
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
            _firstLaunchState = firstLaunchState ?? throw new ArgumentNullException(nameof(firstLaunchState));
        }

        public async UniTask Enter()
        {
            await _platformInitializer.InitializeAsync();

            await _levelCatalog.LoadAsync(CancellationToken.None);

            _analytics.ReportEvent(new GameReadyEvent());

            SceneId target = SceneId.Menu;

            if (_firstLaunchState.IsFirstLaunch)
            {
                _firstLaunchState.MarkLaunched();
                target = SceneId.Game;
            }

            _stateMachine.Enter<LoadSceneState, LoadSceneState.Payload>
            (new LoadSceneState.Payload(target));
        }

        public UniTask Exit() => 
            UniTask.CompletedTask;
    }
}
