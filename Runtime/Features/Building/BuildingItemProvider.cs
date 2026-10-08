using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Core.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Features.Building
{
    public class BuildingItemProvider : IDisposable
    {
        public const string Label = "buildingitem";

        private readonly IAssetProvider _assets;
        private readonly Dictionary<int, BuildingItemView> _prefabs = new Dictionary<int, BuildingItemView>();

        public BuildingItemProvider(IAssetProvider assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public bool IsLoaded { get; private set; }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (IsLoaded)
            {
                return;
            }

            IReadOnlyList<GameObject> loaded = await _assets.LoadByLabelAsync<GameObject>(Label, cancellationToken);

            _prefabs.Clear();

            foreach (GameObject go in loaded)
            {
                if (go == null)
                {
                    continue;
                }

                BuildingItemView view = go.GetComponent<BuildingItemView>();

                if (view != null)
                {
                    _prefabs[view.ItemId] = view;
                }
            }

            IsLoaded = true;
        }

        public bool Contains(int itemId)
        {
            return _prefabs.ContainsKey(itemId);
        }

        public BuildingItemView Get(int itemId)
        {
            if (_prefabs.TryGetValue(itemId, out BuildingItemView view))
            {
                return view;
            }

            throw new KeyNotFoundException($"Building item prefab not loaded: {itemId}");
        }

        public void Dispose()
        {
            _prefabs.Clear();
            IsLoaded = false;
            _assets.Release(Label);
        }
    }
}
