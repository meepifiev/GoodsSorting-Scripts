using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Features.Legacy.Levels
{
    [CreateAssetMenu(
        fileName = "LegacyLevelConfig",
        menuName = "Legacy/Level Config")]
    public class LegacyLevelConfig : ScriptableObject
    {
        public LevelData level;

        [SerializeField] private bool _disabled;

        public bool IsDisabled => _disabled;

        public void SetDisabled(bool disabled)
        {
            _disabled = disabled;
        }
    }

    [Serializable]
    public class LevelData
    {
        public int timeToPlay;
        public int numberCellLock;
        public int itemHiden;
        public List<CellData> cells = new();
        public List<MechanicData> mechanics = new();
    }

    [Serializable]
    public class CellData
    {
        public int posX;
        public int posY;
        public int cellType;
        public int numberLayer;
        public int moveType;
        public int speed;
        public int isLock;
        public List<ItemLayerData> itemsLayer = new();
    }

    [Serializable]
    public class ItemLayerData
    {
        public byte[] items;

        public int[] GetItemIds()
        {
            if (items == null || items.Length == 0)
                return Array.Empty<int>();

            int count = items.Length / sizeof(int);
            int[] itemIds = new int[count];

            for (int i = 0; i < count; i++)
                itemIds[i] = BitConverter.ToInt32(items, i * sizeof(int));

            return itemIds;
        }
    }

    [Serializable]
    public class MechanicData
    {
        public string type;
        public int value;
    }
}
