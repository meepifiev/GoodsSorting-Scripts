using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Core.SceneManagement
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(
            SceneId sceneId,
            Action onLoaded = null,
            CancellationToken cancellationToken = default,
            bool instant = false);
    }
}
