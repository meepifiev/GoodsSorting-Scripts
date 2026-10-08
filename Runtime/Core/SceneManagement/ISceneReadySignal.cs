using Cysharp.Threading.Tasks;

namespace _Project.Core.SceneManagement
{
    public interface ISceneReadySignal
    {
        void BeginWaiting();

        void SetReady();

        UniTask WaitAsync();
    }
}
