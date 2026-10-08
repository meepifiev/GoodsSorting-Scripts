using System;

namespace _Project.Features.Services.InactivityHintServices
{
    public class AbilityHintServiceStub : IAbilityHintService
    {
        public event Action InactivityHintStarted;
        public event Action InactivityHintStopped;

        public void StartInactivityHint()
        {
            InactivityHintStarted?.Invoke();
        }

        public void StopInactivityHint()
        {
            InactivityHintStopped?.Invoke();
        }
    }
}
