using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Features.Building
{
    [CreateAssetMenu(fileName = "BuildingItemCatalog", menuName = "_Project/Building/Item Catalog", order = 2)]
    public class BuildingItemCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private int _id;
            [SerializeField] private Vector3 _isoSize;

            public Entry(int id, Vector3 isoSize)
            {
                _id = id;
                _isoSize = isoSize;
            }

            public int Id => _id;
            public Vector3 IsoSize => _isoSize;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<int, Entry> _byId;

        public int Count => _entries.Length;

        public Entry GetAt(int index)
        {
            return _entries[index];
        }

        public Entry Get(int itemId)
        {
            EnsureIndex();

            if (_byId.TryGetValue(itemId, out Entry entry) == false)
            {
                throw new KeyNotFoundException(nameof(itemId));
            }

            return entry;
        }

        public bool Contains(int itemId)
        {
            EnsureIndex();
            return _byId.ContainsKey(itemId);
        }

        public void SetEntries(Entry[] entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _byId = null;
        }

        private void EnsureIndex()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<int, Entry>(_entries.Length);

            foreach (Entry entry in _entries)
            {
                _byId[entry.Id] = entry;
            }
        }
    }
}
