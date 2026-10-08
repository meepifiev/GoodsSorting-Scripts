using System;
using _Project.Core.SceneCreation;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using _Project.Core.StateMachine.States.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Composition.Startup.States
{
    public class GameSceneState : IPayloadState<GameSceneState.Payload>
    {
        private readonly IGameSceneLoadService _gameSceneLoadService;
        private readonly IGameStateMachine _gameStateMachine;
        private readonly ISceneReadySignal _sceneReadySignal;

        public GameSceneState
        (
            IGameSceneLoadService gameSceneLoadService,
            IGameStateMachine gameStateMachine,
            ISceneReadySignal sceneReadySignal
        )
        {
            _gameSceneLoadService = gameSceneLoadService ?? throw new ArgumentNullException(nameof(gameSceneLoadService));
            _gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
            _sceneReadySignal = sceneReadySignal ?? throw new ArgumentNullException(nameof(sceneReadySignal));
        }

        public async UniTask Enter(Payload payload)
        {
            try
            {
                await _gameSceneLoadService.LoadAsync(payload.Level);

                _sceneReadySignal.SetReady();
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to create game scene. level = {payload.Level}");

                _sceneReadySignal.SetReady();
                _gameStateMachine.Enter<LoadSceneState, LoadSceneState.Payload>
                    (new LoadSceneState.Payload(SceneId.Menu));
            }
        }

        public UniTask Exit() => 
            UniTask.CompletedTask;

        public struct Payload
        {
            public readonly int Level;

            public Payload(int level)
            {
                Level = level;
            }
        }
    }
}
