using System;
using UnityEngine;

namespace _Project.Features.Legacy
{
    [Serializable]
    public class LegacyItemData
    {
        [Tooltip("Item ID from legacy level configs.")]
        public int id;

        [Tooltip("Prefab used for this item ID.")]
        public GameObject prefab;
    }
}
