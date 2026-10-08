using System.Collections.Generic;
using UnityEngine;

namespace _Project.Features.Legacy
{
    [CreateAssetMenu(
        fileName = "LegacyItemDatabase",
        menuName = "Legacy/Item Database")]
    public class LegacyItemDatabase : ScriptableObject
    {
        [SerializeField]
        private List<LegacyItemData> _items = new();

        private Dictionary<int, GameObject> _itemsById;

        private void OnEnable()
        {
            BuildCache();
        }

        public GameObject GetPrefab(int itemId)
        {
            if (itemId == 0)
                return null;

            if (_itemsById == null)
                BuildCache();

            if (_itemsById.TryGetValue(itemId, out GameObject prefab))
                return prefab;

            Debug.LogWarning(
                $"[LegacyItemDatabase] Prefab for Item ID {itemId} not found.",
                this);

            return null;
        }

        private void BuildCache()
        {
            _itemsById = new Dictionary<int, GameObject>();

            foreach (LegacyItemData item in _items)
            {
                if (item == null)
                    continue;

                if (item.id == 0)
                    continue;

                if (_itemsById.ContainsKey(item.id))
                {
                    Debug.LogWarning(
                        $"Duplicate Item ID: {item.id}",
                        this);

                    continue;
                }

                _itemsById.Add(item.id, item.prefab);
            }
        }
    }
}
