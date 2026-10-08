using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Core.Level
{
    public interface ILevelCatalog
    {
        bool IsLoaded { get; }

        int Count { get; }

        bool IsPlayable(int levelNumber);

        int FindPlayable(int levelNumber, int direction);

        string GetAddress(int levelNumber);

        UniTask LoadAsync(CancellationToken cancellationToken);
    }
}
