using System;

namespace _Project.Core.Progress
{
    public interface IWinsStorage
    {
        event Action Changed;

        int Wins { get; }

        void AddWin();
    }
}
