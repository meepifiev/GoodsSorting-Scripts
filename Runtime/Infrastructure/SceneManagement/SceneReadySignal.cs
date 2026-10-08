using _Project.Core.SceneManagement;
using Cysharp.Threading.Tasks;

namespace _Project.Infrastructure.SceneManagement
{
    public class SceneReadySignal : ISceneReadySignal
    {
        private UniTaskCompletionSource _completion;

        public void BeginWaiting()
        {
            _completion = new UniTaskCompletionSource();
        }

        public void SetReady()
        {
            _completion?.TrySetResult();
        }

        public UniTask WaitAsync()
        {
            return _completion != null ? _completion.Task : UniTask.CompletedTask;
        }
    }
}
