using _Project.Core.SceneManagement;
using UnityEngine;

namespace _Project.Infrastructure.SceneManagement
{
    public class LoadingCurtain : ILoadingCurtain
    {
        private const string PrefabPath = "LoadingCurtain";

        private ILoadingCurtain _view;
        private bool _resolved;

        public void Show()
        {
            EnsureBuilt();
            _view?.Show();
        }

        public void Hide()
        {
            _view?.Hide();
        }

        private void EnsureBuilt()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            GameObject prefab = Resources.Load<GameObject>(PrefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"[LoadingCurtain] Prefab '{PrefabPath}' not found in Resources.");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(instance);

            _view = instance.GetComponentInChildren<ILoadingCurtain>(true);

            if (_view == null)
            {
                Debug.LogWarning("[LoadingCurtain] Instantiated prefab has no ILoadingCurtain view.");
            }
        }
    }
}
