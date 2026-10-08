using System;

namespace _Project.Infrastructure.SceneManagement
{
    public interface ITransitionFactory
    {
        void Create(bool invert, bool autoDestroy, Action onComplete = null);
    }
}
