using System;

namespace _Project.Core.Lives
{
    public interface ILivesService
    {
        event Action Changed;

        int Current { get; }
        int Max { get; }
        float SecondsToNextLife { get; }
        bool IsInfinite { get; }

        bool TrySpendLife();
        void AddLife();
        void Fill();
        void SetInfinite(bool value);
    }
}
