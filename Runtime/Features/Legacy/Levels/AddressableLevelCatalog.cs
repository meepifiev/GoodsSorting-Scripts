using System;
using System.Threading;
using _Project.Core.Assets;
using _Project.Core.Level;
using Cysharp.Threading.Tasks;

namespace _Project.Features.Legacy.Levels
{
    public class AddressableLevelCatalog : ILevelCatalog
    {
        public const string ManifestAddress = "LevelManifest";

        private readonly IAssetProvider _assets;

        private LevelManifest _manifest;

        public AddressableLevelCatalog(IAssetProvider assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public bool IsLoaded => _manifest != null;

        public int Count => _manifest != null ? _manifest.Count : 0;

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (_manifest != null)
            {
                return;
            }

            _manifest = await _assets.LoadAsync<LevelManifest>(ManifestAddress, cancellationToken);
        }

        public bool IsPlayable(int levelNumber)
        {
            if (_manifest == null || _manifest.TryGet(levelNumber, out LevelManifest.Entry entry) == false)
            {
                return false;
            }

            return entry.disabled == false && string.IsNullOrEmpty(entry.address) == false;
        }

        public int FindPlayable(int levelNumber, int direction)
        {
            if (_manifest == null)
            {
                return 0;
            }

            if (direction == 0)
            {
                return IsPlayable(levelNumber) ? levelNumber : 0;
            }

            int step = direction > 0 ? 1 : -1;

            for (int current = levelNumber; current >= 1 && current <= _manifest.Count; current += step)
            {
                if (IsPlayable(current))
                {
                    return current;
                }
            }

            return 0;
        }

        public string GetAddress(int levelNumber)
        {
            if (_manifest != null && _manifest.TryGet(levelNumber, out LevelManifest.Entry entry))
            {
                return entry.address;
            }

            return null;
        }
    }
}
