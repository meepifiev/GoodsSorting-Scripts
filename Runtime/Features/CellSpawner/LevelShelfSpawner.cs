using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Features.Legacy;
using _Project.Features.Legacy.Levels;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class LevelShelfSpawner : MonoBehaviour
    {
        private readonly List<ShelfCellView> _spawnedCells = new();
        private readonly List<ShelfItemView> _spawnedItems = new();

        private bool _destroyed;

        [Header("Items")] [SerializeField] private LegacyItemDatabase _itemDatabase;

        [SerializeField] private ShelfItemShadowConfig _shadowConfig;

        [Header("Cell Prefabs")] [Tooltip("Default cell. CellType = 0")] [SerializeField]
        private ShelfCellView _type0Prefab;

        [Tooltip("CellType = 1")] [SerializeField]
        private ShelfCellView _type1Prefab;

        [Tooltip("CellType = 2")] [SerializeField]
        private ShelfCellView _type2Prefab;

        [Tooltip("CellType = 3")] [SerializeField]
        private ShelfCellView _type3Prefab;

        [Header("Hierarchy")] [SerializeField] private Transform _levelRoot;

        public event Action<int> LevelSpawned;
        public event Action<int> ItemsRemoved;
        public event Action<Vector3, int> Matched;
        public event Action LevelCleared;
        public event Action Deadlocked;

        private bool _deadlockCheckPending;

        public bool HasSpawnedLevel { get; private set; }
        public IReadOnlyList<ShelfCellView> SpawnedCells => _spawnedCells;

        public void Spawn(LegacyLevelConfig legacyLevelConfig)
        {
            if (legacyLevelConfig == null)
            {
                throw new ArgumentNullException(nameof(legacyLevelConfig));
            }

            if (legacyLevelConfig.level == null)
            {
                throw new InvalidOperationException(nameof(legacyLevelConfig.level));
            }

            if (legacyLevelConfig.level.cells == null)
            {
                throw new InvalidOperationException(nameof(legacyLevelConfig.level.cells));
            }

            ClearLevel();

            foreach (CellData cellData in legacyLevelConfig.level.cells)
                SpawnCell(cellData);

            SubscribeToItems();
            HasSpawnedLevel = true;
            OnLevelSpawned(GetCurrentItemCount());
        }

        [ContextMenu("Clear Level")]
        public void ClearLevel()
        {
            HasSpawnedLevel = false;
            OnLevelCleared();
            UnsubscribeFromCells();
            UnsubscribeFromItems();

            if (_levelRoot == null)
                return;

            for (int i = _levelRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _levelRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private void SpawnCell(CellData cellData)
        {
            ShelfCellView prefab = GetPrefab(cellData.cellType);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[LevelShelfSpawner] Prefab for CellType {cellData.cellType} is not assigned. Cell ({cellData.posX}, {cellData.posY}) skipped.",
                    this);

                return;
            }

            ShelfCellView cell = Instantiate(prefab, _levelRoot);
            cell.transform.localPosition = new Vector3(cellData.posX, cellData.posY, 0f);
            cell.transform.localRotation = Quaternion.identity;
            cell.Initialize(cellData, _itemDatabase, _shadowConfig);
            cell.Matched += OnCellMatched;
            _spawnedCells.Add(cell);
        }

        private void OnCellMatched(Vector3 worldPosition, int itemCount)
        {
            Matched?.Invoke(worldPosition, itemCount);
        }

        public ShelfItemView ReplaceFrontItem(ShelfItemView oldItem, int newItemId)
        {
            if (oldItem == null)
            {
                return null;
            }

            ShelfCellView cell = oldItem.Cell;

            if (cell == null || cell.IsResolvingMatch)
            {
                return null;
            }

            if (oldItem.IsFrontLayer == false || oldItem.IsRemoving)
            {
                return null;
            }

            if (cell.CanReplaceItem(newItemId) == false)
            {
                return null;
            }

            oldItem.Destroyed -= OnItemDestroyed;
            _spawnedItems.Remove(oldItem);

            ShelfItemView newItem = cell.ReplaceFrontItem(oldItem, newItemId);

            if (newItem == null)
            {
                oldItem.Destroyed += OnItemDestroyed;
                _spawnedItems.Add(oldItem);

                return null;
            }

            newItem.Destroyed += OnItemDestroyed;
            _spawnedItems.Add(newItem);

            return newItem;
        }

        public int GetCurrentItemCount()
        {
            if (_levelRoot == null)
            {
                return 0;
            }

            ShelfItemView[] items = _levelRoot.GetComponentsInChildren<ShelfItemView>(true);
            int itemCount = 0;

            foreach (ShelfItemView item in items)
            {
                if (item != null && item.IsRemoving == false)
                {
                    itemCount++;
                }
            }

            return itemCount;
        }

        private void UnsubscribeFromCells()
        {
            foreach (ShelfCellView cell in _spawnedCells)
            {
                if (cell != null)
                {
                    cell.Matched -= OnCellMatched;
                }
            }

            _spawnedCells.Clear();
        }

        private void SubscribeToItems()
        {
            if (_levelRoot == null)
            {
                return;
            }

            ShelfItemView[] items = _levelRoot.GetComponentsInChildren<ShelfItemView>(true);

            foreach (ShelfItemView item in items)
            {
                item.Destroyed += OnItemDestroyed;
                _spawnedItems.Add(item);
            }
        }

        private void UnsubscribeFromItems()
        {
            foreach (ShelfItemView item in _spawnedItems)
            {
                if (item != null)
                {
                    item.Destroyed -= OnItemDestroyed;
                }
            }

            _spawnedItems.Clear();
        }

        private void OnDestroy()
        {
            _destroyed = true;
        }

        private void OnItemDestroyed()
        {
            if (_destroyed || gameObject.scene.isLoaded == false)
            {
                return;
            }

            ItemsRemoved?.Invoke(1);
            ScheduleDeadlockCheck();
        }

        public bool HasAnyFreeFrontSlot()
        {
            foreach (ShelfCellView cell in _spawnedCells)
            {
                if (cell != null && cell.HasFreeFrontSlot())
                    return true;
            }

            return false;
        }

        public int FreeSpace(int maxItems)
        {
            if (maxItems <= 0)
                return 0;

            int removed = 0;

            foreach (ShelfCellView cell in _spawnedCells)
            {
                if (removed >= maxItems)
                    break;

                if (cell == null || cell.IsResolvingMatch)
                    continue;

                List<ShelfItemView> reserved = cell.ReserveFrontLayerItems(maxItems - removed);

                if (reserved.Count == 0)
                    continue;

                cell.BreakReservedItems(reserved);
                removed += reserved.Count;
            }

            return removed;
        }

        public void ScheduleDeadlockCheck()
        {
            if (_deadlockCheckPending || _destroyed || HasSpawnedLevel == false)
                return;

            _deadlockCheckPending = true;
            StartCoroutine(DeadlockCheckRoutine());
        }

        private IEnumerator DeadlockCheckRoutine()
        {
            yield return null;
            yield return null;

            _deadlockCheckPending = false;

            if (_destroyed || HasSpawnedLevel == false)
                yield break;

            if (HasAnyFreeFrontSlot())
                yield break;

            if (IsAnyCellResolvingMatch())
            {
                ScheduleDeadlockCheck();
                yield break;
            }

            Deadlocked?.Invoke();
        }

        private bool IsAnyCellResolvingMatch()
        {
            foreach (ShelfCellView cell in _spawnedCells)
            {
                if (cell != null && cell.IsResolvingMatch)
                    return true;
            }

            return false;
        }

        private void OnLevelSpawned(int itemCount)
        {
            LevelSpawned?.Invoke(itemCount);
        }

        private void OnLevelCleared()
        {
            LevelCleared?.Invoke();
        }

        private ShelfCellView GetPrefab(int cellType)
        {
            switch (cellType)
            {
                case 0:
                    return _type0Prefab;

                case 1:
                    return _type1Prefab;

                case 2:
                    return _type2Prefab;

                case 3:
                    return _type3Prefab;

                default:
                    Debug.LogError(
                        $"[LevelShelfSpawner] Unknown CellType: {cellType}",
                        this);

                    return null;
            }
        }
    }
}
