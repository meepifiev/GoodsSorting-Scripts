using _Project.Core.StateMachine;
using _Project.Core.StateMachine.States.Interfaces;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup.States
{
    public class MainMenuSceneState : IState
    {
        public UniTask Enter() => 
            UniTask.CompletedTask;

        public UniTask Exit() => 
            UniTask.CompletedTask;
    }
}
