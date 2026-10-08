using System;
using _Project.Composition.Startup.States;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;

namespace _Project.Composition.Startup
{
    public class GameLauncher : IGameLauncher
    {
        private readonly IGameStateMachine _gameStateMachine;

        public GameLauncher(IGameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine ?? throw new ArgumentNullException(nameof(gameStateMachine));
        }

        public void StartGame(bool instant = false)
        {
            _gameStateMachine.Enter<LoadSceneState, LoadSceneState.Payload>(
                new LoadSceneState.Payload(SceneId.Game, instant));
        }

        public void GoToMenu()
        {
            _gameStateMachine.Enter<LoadSceneState, LoadSceneState.Payload>(
                new LoadSceneState.Payload(SceneId.Menu));
        }

        public void GoToBuilding()
        {
            _gameStateMachine.Enter<LoadSceneState, LoadSceneState.Payload>(
                new LoadSceneState.Payload(SceneId.Building));
        }
    }
}
