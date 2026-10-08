using System;
using System.Threading;
using _Project.Core.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Infrastructure.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        private const float SceneReadyTimeoutSeconds = 20f;

        private readonly ITransitionFactory _transitionFactory;
        private readonly ISceneReadySignal _sceneReadySignal;
        private readonly ILoadingCurtain _loadingCurtain;

        public SceneLoader(
            ITransitionFactory transitionFactory,
            ISceneReadySignal sceneReadySignal,
            ILoadingCurtain loadingCurtain)
        {
            _transitionFactory = transitionFactory ?? throw new ArgumentNullException(nameof(transitionFactory));
            _sceneReadySignal = sceneReadySignal ?? throw new ArgumentNullException(nameof(sceneReadySignal));
            _loadingCurtain = loadingCurtain ?? throw new ArgumentNullException(nameof(loadingCurtain));
        }

        public UniTask LoadAsync(
            SceneId sceneId,
            Action onLoaded = null,
            CancellationToken cancellationToken = default,
            bool instant = false)
        {
            UniTaskCompletionSource completion = new UniTaskCompletionSource();

            if (instant)
            {
                LoadAndOpenScene(sceneId, onLoaded, completion, cancellationToken, true, false).Forget();
                return completion.Task.AttachExternalCancellation(cancellationToken);
            }

            bool awaitSceneReady = RequiresSceneReady(sceneId);

            if (awaitSceneReady)
            {
                _sceneReadySignal.BeginWaiting();
            }

            _transitionFactory.Create(
                invert: false,
                autoDestroy: false,
                onComplete: () => LoadAndOpenScene(sceneId, onLoaded, completion, cancellationToken, false, awaitSceneReady).Forget());

            return completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private async UniTask LoadAndOpenScene(
            SceneId sceneId,
            Action onLoaded,
            UniTaskCompletionSource completion,
            CancellationToken cancellationToken,
            bool instant,
            bool awaitSceneReady)
        {
            if (awaitSceneReady)
            {
                _loadingCurtain.Show();
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId.ToString(), LoadSceneMode.Single);

            if (operation == null)
            {
                _loadingCurtain.Hide();
                completion.TrySetException(new InvalidOperationException(nameof(sceneId)));
                return;
            }

            await operation.ToUniTask(cancellationToken: cancellationToken);

            onLoaded?.Invoke();

            if (awaitSceneReady)
            {
                await WaitForSceneReady();
            }

            if (instant == false)
            {
                _transitionFactory.Create(invert: true, autoDestroy: true);
            }

            if (awaitSceneReady)
            {
                _loadingCurtain.Hide();
            }

            completion.TrySetResult();
        }

        private async UniTask WaitForSceneReady()
        {
            await UniTask.WhenAny(
                _sceneReadySignal.WaitAsync(),
                UniTask.Delay(TimeSpan.FromSeconds(SceneReadyTimeoutSeconds), ignoreTimeScale: true));
        }

        private static bool RequiresSceneReady(SceneId sceneId)
        {
            return sceneId == SceneId.Game || sceneId == SceneId.Building;
        }
    }
}
