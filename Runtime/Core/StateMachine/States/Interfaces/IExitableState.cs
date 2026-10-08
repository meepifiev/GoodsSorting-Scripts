using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine.States.Interfaces
{
    public interface IExitableState
    {
        UniTask Exit();
    }
}
