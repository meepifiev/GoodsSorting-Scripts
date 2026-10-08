using System;
using _Project.Core.SceneCreation;
using Cysharp.Threading.Tasks;

namespace _Project.Infrastructure.SceneManagement
{
    public class GameSceneLoadService : IGameSceneLoadService
    {
        private IGameSceneFactory _gameSceneFactory;

        public void Register(IGameSceneFactory gameSceneFactory)
        {
            if (gameSceneFactory == null)
            {
                throw new ArgumentNullException(nameof(gameSceneFactory));
            }

            if (_gameSceneFactory != null)
            {
                throw new InvalidOperationException(nameof(_gameSceneFactory));
            }

            _gameSceneFactory = gameSceneFactory;
        }

        public void Unregister(IGameSceneFactory gameSceneFactory)
        {
            if (ReferenceEquals(_gameSceneFactory, gameSceneFactory) == false)
            {
                return;
            }

            _gameSceneFactory = null;
        }

        public UniTask LoadAsync(int level)
        {
            if (_gameSceneFactory == null)
            {
                throw new InvalidOperationException(nameof(_gameSceneFactory));
            }

            return _gameSceneFactory.Create(level);
        }
    }
}
