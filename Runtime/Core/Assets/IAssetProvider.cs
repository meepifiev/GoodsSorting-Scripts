using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Core.Assets
{
    public interface IAssetProvider
    {
        UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object;

        UniTask<IReadOnlyList<T>> LoadByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object;

        void Release(string key);

        void ReleaseAll();
    }
}
