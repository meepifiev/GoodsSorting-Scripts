using System;
using System.Threading;
using _Project.Core.SceneManagement;
using _Project.Features.Building;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace _Project.UI.Building
{
    public class BuildingContentBootstrapper : IStartable
    {
        private readonly BuildingEnvironmentProvider _environment;
        private readonly BuildingItemsPresenter _items;
        private readonly BuildingFlowPresenter _flow;
        private readonly ISceneReadySignal _sceneReadySignal;

        public BuildingContentBootstrapper(
            BuildingEnvironmentProvider environment,
            BuildingItemsPresenter items,
            BuildingFlowPresenter flow,
            ISceneReadySignal sceneReadySignal)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _sceneReadySignal = sceneReadySignal ?? throw new ArgumentNullException(nameof(sceneReadySignal));
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            try
            {
                await _environment.LoadAsync(CancellationToken.None);
                await _items.LoadAndBuildAsync(CancellationToken.None);
                _flow.Build();
            }
            finally
            {
                _sceneReadySignal.SetReady();
            }
        }
    }
}
