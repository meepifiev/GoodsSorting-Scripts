using System;
using System.Threading;
using _Project.Core.Assets;
using _Project.Core.Level;
using Cysharp.Threading.Tasks;

namespace _Project.Features.Legacy.Levels
{
    public class LegacyLevelLoadingService : ILevelLoadingService<LegacyLevelConfig>, IDisposable
    {
        private readonly ILevelCatalog _levelCatalog;
        private readonly IAssetProvider _assets;

        private string _currentAddress;

        public LegacyLevelLoadingService(ILevelCatalog levelCatalog, IAssetProvider assets)
        {
            _levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public async UniTask<LegacyLevelConfig> LoadAsync(int levelIndex)
        {
            string address = _levelCatalog.GetAddress(levelIndex);

            if (string.IsNullOrEmpty(address))
            {
                throw new InvalidOperationException($"Level {levelIndex} has no content address in the manifest.");
            }

            ReleaseCurrent();

            LegacyLevelConfig config = await _assets.LoadAsync<LegacyLevelConfig>(address, CancellationToken.None);
            _currentAddress = address;
            return config;
        }

        public void Dispose()
        {
            ReleaseCurrent();
        }

        private void ReleaseCurrent()
        {
            if (_currentAddress != null)
            {
                _assets.Release(_currentAddress);
                _currentAddress = null;
            }
        }
    }
}
