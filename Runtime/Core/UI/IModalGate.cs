using System;

namespace _Project.Core.UI
{
    public interface IModalGate
    {
        bool IsOpen { get; }

        event Action<bool> Changed;

        void Open();

        void Close();
    }
}
