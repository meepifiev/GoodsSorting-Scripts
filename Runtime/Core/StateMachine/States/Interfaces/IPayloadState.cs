using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine.States.Interfaces
{
    public interface IPayloadState<in TPayload> : IExitableState
    {
        UniTask Enter(TPayload payload);
    }
}
