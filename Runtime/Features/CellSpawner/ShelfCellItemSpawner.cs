using _Project.Features.Legacy;
using _Project.Features.Legacy.Levels;
using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfCellItemSpawner
    {
        private const float ItemScaleMultiplier = 1.6f;

        private readonly ShelfCellView _cell;
        private readonly ShelfLayerView _layerPrefab;
        private readonly Transform _layersRoot;
        private readonly LegacyItemDatabase _itemDatabase;
        private readonly Vector3 _layerOffset;
        private readonly ShelfLayerVisuals _visuals;
        private readonly ShelfItemShadowConfig _shadowConfig;

        public ShelfCellItemSpawner(
            ShelfCellView cell,
            ShelfLayerView layerPrefab,
            Transform layersRoot,
            LegacyItemDatabase itemDatabase,
            Vector3 layerOffset,
            ShelfLayerVisuals visuals,
            ShelfItemShadowConfig shadowConfig)
        {
            _cell = cell;
            _layerPrefab = layerPrefab;
            _layersRoot = layersRoot;
            _itemDatabase = itemDatabase;
            _layerOffset = layerOffset;
            _visuals = visuals;
            _shadowConfig = shadowConfig;
        }

        public void SpawnLayers(CellData cellData)
        {
            if (cellData.itemsLayer == null)
                return;

            if (_layerPrefab == null)
                return;

            for (int layerIndex = 0; layerIndex < cellData.itemsLayer.Count; layerIndex++)
            {
                ShelfLayerView layer = Object.Instantiate(_layerPrefab, _layersRoot);
                layer.transform.localPosition = _layerOffset * layerIndex;
                layer.transform.localRotation = Quaternion.identity;

                SpawnItemsOnLayer(layer, cellData.itemsLayer[layerIndex]);
            }
        }

        public ShelfItemView SpawnItemInSlot(
            ShelfLayerView layer,
            Transform slot,
            int itemId,
            int layerIndex)
        {
            if (slot == null || _itemDatabase == null)
                return null;

            GameObject prefab = _itemDatabase.GetPrefab(itemId);

            if (prefab == null)
                return null;

            GameObject item = Object.Instantiate(prefab, slot);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale *= ItemScaleMultiplier;
            ShelfItemView itemView = item.GetComponent<ShelfItemView>();

            if (itemView == null)
                itemView = item.AddComponent<ShelfItemView>();

            ShelfItemShadowView shadowView = item.GetComponent<ShelfItemShadowView>();

            if (shadowView == null)
                shadowView = item.AddComponent<ShelfItemShadowView>();

            EnsureItemSelectionVisual(itemView);
            shadowView.SetConfig(_shadowConfig);
            shadowView.EnsureBuilt();
            itemView.SetItemId(itemId);
            itemView.Bind(
                _cell,
                layer,
                slot,
                layerIndex);

            _visuals.RegisterDefaults(item);

            return itemView;
        }

        public void EnsureItemShadow(ShelfItemView item)
        {
            ShelfItemShadowView shadowView = item.GetComponent<ShelfItemShadowView>();

            if (shadowView == null)
                shadowView = item.gameObject.AddComponent<ShelfItemShadowView>();

            shadowView.SetConfig(_shadowConfig);
            shadowView.EnsureBuilt();
        }

        public void EnsureItemSelectionVisual(ShelfItemView item)
        {
            ShelfItemSelectionVisualView selectionVisualView = item.GetComponent<ShelfItemSelectionVisualView>();

            if (selectionVisualView == null)
                item.gameObject.AddComponent<ShelfItemSelectionVisualView>();
        }

        private void SpawnItemsOnLayer(ShelfLayerView layer, ItemLayerData layerData)
        {
            if (layerData == null || _itemDatabase == null)
                return;

            int[] itemIds = layerData.GetItemIds();

            for (int i = 0; i < itemIds.Length && i < layer.Slots.Length; i++)
            {
                SpawnItemInSlot(
                    layer,
                    layer.Slots[i],
                    itemIds[i],
                    layer.transform.GetSiblingIndex());
            }
        }
    }
}
