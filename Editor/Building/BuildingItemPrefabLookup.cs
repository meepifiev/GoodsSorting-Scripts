using System.Collections.Generic;
using _Project.Features.Building;
using UnityEditor;

namespace _Project.Editor.Building
{
    public static class BuildingItemPrefabLookup
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/Building";

        private static Dictionary<int, BuildingItemView> _cache;

        public static BuildingItemView Get(int itemId)
        {
            EnsureCache();
            return _cache.TryGetValue(itemId, out BuildingItemView view) ? view : null;
        }

        public static void Invalidate()
        {
            _cache = null;
        }

        private static void EnsureCache()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new Dictionary<int, BuildingItemView>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BuildingItemView view = AssetDatabase.LoadAssetAtPath<BuildingItemView>(path);

                if (view != null)
                {
                    _cache[view.ItemId] = view;
                }
            }
        }
    }
}
