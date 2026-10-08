using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Core.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace _Project.Infrastructure.Assets
{
    public class AddressablesAssetProvider : IAssetProvider, IDisposable
    {
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

        public async UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            _handles[address] = handle;

            T result = await handle.ToUniTask(cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException($"Asset not found at address '{address}'.");
            }

            return result;
        }

        public async UniTask<IReadOnlyList<T>> LoadByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>((object)label, null);
            _handles[label] = handle;

            IList<T> loaded = await handle.ToUniTask(cancellationToken: cancellationToken);
            return new List<T>(loaded);
        }

        public void Release(string key)
        {
            if (_handles.TryGetValue(key, out AsyncOperationHandle handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                _handles.Remove(key);
            }
        }

        public void ReleaseAll()
        {
            foreach (AsyncOperationHandle handle in _handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }
    }
}
