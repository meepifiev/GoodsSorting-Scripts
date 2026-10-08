using System;

namespace _Project.Core.Score
{
    public interface IStarsCounter
    {
        int Stars { get; }
        event Action<int> Changed;
    }
}
