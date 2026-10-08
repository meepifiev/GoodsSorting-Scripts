using Cysharp.Threading.Tasks;

namespace _Project.Core.SceneCreation
{
    public interface IGameSceneFactory
    {
        UniTask Create(int level);
    }
}
