using Cysharp.Threading.Tasks;

namespace _Project.Core.Level
{
    public interface ILevelLoadingService<TLevelConfig>
    {
        UniTask<TLevelConfig> LoadAsync(int levelIndex);
    }
}
