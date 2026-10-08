using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingTaskConfig
    {
        [SerializeField] private string _titleKey;
        [SerializeField] private BuildingTaskType _type;
        [SerializeField] private int _dependsOnTaskIndex = -1;
        [SerializeField] private BuildingItemPlacement[] _itemsBefore = Array.Empty<BuildingItemPlacement>();
        [SerializeField] private BuildingItemPlacement[] _itemsAfter = Array.Empty<BuildingItemPlacement>();

        public BuildingTaskConfig(
            string titleKey,
            BuildingTaskType type,
            int dependsOnTaskIndex,
            BuildingItemPlacement[] itemsBefore,
            BuildingItemPlacement[] itemsAfter)
        {
            _titleKey = titleKey;
            _type = type;
            _dependsOnTaskIndex = dependsOnTaskIndex;
            _itemsBefore = itemsBefore ?? throw new ArgumentNullException(nameof(itemsBefore));
            _itemsAfter = itemsAfter ?? throw new ArgumentNullException(nameof(itemsAfter));
        }

        public string TitleKey => _titleKey;
        public BuildingTaskType Type => _type;
        public int DependsOnTaskIndex => _dependsOnTaskIndex;
        public bool HasDependency => _dependsOnTaskIndex >= 0;
        public BuildingItemPlacement[] ItemsBefore => _itemsBefore;
        public BuildingItemPlacement[] ItemsAfter => _itemsAfter;

        public void SetDependsOnTaskIndex(int taskIndex)
        {
            _dependsOnTaskIndex = taskIndex;
        }

        public void SetItemsBefore(BuildingItemPlacement[] items)
        {
            _itemsBefore = items ?? throw new ArgumentNullException(nameof(items));
        }

        public void SetItemsAfter(BuildingItemPlacement[] items)
        {
            _itemsAfter = items ?? throw new ArgumentNullException(nameof(items));
        }
    }
}
