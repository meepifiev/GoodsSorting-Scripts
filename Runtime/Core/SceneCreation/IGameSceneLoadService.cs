using Cysharp.Threading.Tasks;

namespace _Project.Core.SceneCreation
{
    public interface IGameSceneLoadService
    {
        void Register(IGameSceneFactory gameSceneFactory);
        void Unregister(IGameSceneFactory gameSceneFactory);
        UniTask LoadAsync(int level);
    }
}
