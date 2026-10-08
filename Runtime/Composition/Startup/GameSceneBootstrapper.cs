using System;
using _Project.Core.SceneCreation;
using VContainer.Unity;

namespace _Project.Composition.Startup
{
    public class GameSceneBootstrapper : IInitializable, IDisposable
    {
        private readonly IGameSceneFactory _gameSceneFactory;
        private readonly IGameSceneLoadService _gameSceneLoadService;

        public GameSceneBootstrapper(
            IGameSceneFactory gameSceneFactory,
            IGameSceneLoadService gameSceneLoadService)
        {
            _gameSceneFactory = gameSceneFactory ?? throw new ArgumentNullException(nameof(gameSceneFactory));
            _gameSceneLoadService = gameSceneLoadService ?? throw new ArgumentNullException(nameof(gameSceneLoadService));
        }

        public void Initialize()
        {
            _gameSceneLoadService.Register(_gameSceneFactory);
        }

        public void Dispose()
        {
            _gameSceneLoadService.Unregister(_gameSceneFactory);
        }
    }
}
