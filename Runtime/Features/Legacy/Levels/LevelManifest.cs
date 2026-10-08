using System;
using UnityEngine;

namespace _Project.Features.Legacy.Levels
{
    [CreateAssetMenu(fileName = "LevelManifest", menuName = "_Project/Levels/Level Manifest", order = 1)]
    public class LevelManifest : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string address;
            public bool disabled;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public int Count => _entries.Length;

        public bool TryGet(int levelNumber, out Entry entry)
        {
            if (levelNumber < 1 || levelNumber > _entries.Length)
            {
                entry = default;
                return false;
            }

            entry = _entries[levelNumber - 1];
            return true;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }
#endif
    }
}
