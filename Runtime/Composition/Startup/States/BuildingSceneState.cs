using _Project.Core.StateMachine.States.Interfaces;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup.States
{
    public class BuildingSceneState : IState
    {
        public UniTask Enter() =>
            UniTask.CompletedTask;

        public UniTask Exit() =>
            UniTask.CompletedTask;
    }
}
