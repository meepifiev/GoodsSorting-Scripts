using System;
using _Project.Core.Progress;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using _Project.Core.StateMachine.States.Interfaces;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup.States
{
    public class LoadSceneState : IPayloadState<LoadSceneState.Payload>
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly IProgressStorage _progressStorage;

        public LoadSceneState
        (
            IGameStateMachine stateMachine,
            ISceneLoader sceneLoader,
            IProgressStorage progressStorage
        )
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _progressStorage = progressStorage;
        }

        public async UniTask Enter(Payload payload)
        {
            await _sceneLoader.LoadAsync(payload.SceneId, () => SwitchState(payload.SceneId), default, payload.Instant);
        }

        public UniTask Exit() =>
            UniTask.CompletedTask;

        private void SwitchState(SceneId sceneId)
        {
            switch (sceneId)
            {
                case SceneId.Menu:
                    _stateMachine.Enter<MainMenuSceneState>();
                    break;

                case SceneId.Game:
                    _stateMachine.Enter<GameSceneState, GameSceneState.Payload>
                        (new GameSceneState.Payload(_progressStorage.Level));
                    break;

                case SceneId.Building:
                    _stateMachine.Enter<BuildingSceneState>();
                    break;

                case SceneId.Bootstrap:
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null);
            }
        }

        public struct Payload
        {
            public readonly SceneId SceneId;
            public readonly bool Instant;

            public Payload(SceneId sceneId)
                : this(sceneId, false)
            {
            }

            public Payload(SceneId sceneId, bool instant)
            {
                SceneId = sceneId;
                Instant = instant;
            }
        }
    }
}
