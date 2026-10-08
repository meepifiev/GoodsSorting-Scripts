using System;
using System.Threading;
using _Project.Core.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Features.Building
{
    public class BuildingEnvironmentProvider : IDisposable
    {
        public const string Address = "BuildingEnvironment";
        private const string LockedLayerName = "Tilemap_lockarea";

        private readonly IAssetProvider _assets;
        private readonly BuildingWorldView _world;

        private GameObject _instance;

        public BuildingEnvironmentProvider(IAssetProvider assets, BuildingWorldView world)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (_instance != null)
            {
                return;
            }

            GameObject prefab = await _assets.LoadAsync<GameObject>(Address, cancellationToken);
            _instance = UnityEngine.Object.Instantiate(prefab, _world.transform, false);

            Grid grid = _instance.GetComponent<Grid>();
            Tilemap lockedClouds = FindLocked(grid);
            _world.BindEnvironment(grid, lockedClouds);
        }

        public void Dispose()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
            }

            _assets.Release(Address);
        }

        private static Tilemap FindLocked(Grid grid)
        {
            if (grid == null)
            {
                return null;
            }

            foreach (Tilemap tilemap in grid.GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap.name == LockedLayerName)
                {
                    return tilemap;
                }
            }

            return null;
        }
    }
}
