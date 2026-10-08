using System;
using _Project.Core.UI;

namespace _Project.Features.UI
{
    public class ModalGate : IModalGate
    {
        private int _openCount;

        public bool IsOpen => _openCount > 0;

        public event Action<bool> Changed;

        public void Open()
        {
            _openCount++;

            if (_openCount == 1)
            {
                Changed?.Invoke(true);
            }
        }

        public void Close()
        {
            if (_openCount <= 0)
            {
                return;
            }

            _openCount--;

            if (_openCount == 0)
            {
                Changed?.Invoke(false);
            }
        }
    }
}
