using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [CreateAssetMenu(fileName = "BuildingArea", menuName = "_Project/Building/Area Config", order = 0)]
    public class BuildingAreaConfig : ScriptableObject
    {
        [SerializeField] private int _areaIndex;
        [SerializeField] private int _firstGlobalDayIndex;
        [SerializeField] private BuildingDayConfig[] _days = Array.Empty<BuildingDayConfig>();
        [SerializeField] private BuildingItemPlacement[] _decorBefore = Array.Empty<BuildingItemPlacement>();
        [SerializeField] private BuildingItemPlacement[] _decorAfter = Array.Empty<BuildingItemPlacement>();

        public int AreaIndex => _areaIndex;
        public int FirstGlobalDayIndex => _firstGlobalDayIndex;
        public int DayCount => _days.Length;
        public bool HasTasks => _days.Length > 0;
        public BuildingItemPlacement[] DecorBefore => _decorBefore;
        public BuildingItemPlacement[] DecorAfter => _decorAfter;

        public BuildingDayConfig GetDay(int dayIndex)
        {
            if (dayIndex < 0 || dayIndex >= _days.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(dayIndex));
            }

            return _days[dayIndex];
        }

        public void SetDecorBefore(BuildingItemPlacement[] decor)
        {
            _decorBefore = decor ?? throw new ArgumentNullException(nameof(decor));
        }

        public void SetDecorAfter(BuildingItemPlacement[] decor)
        {
            _decorAfter = decor ?? throw new ArgumentNullException(nameof(decor));
        }

        public void Initialize(
            int areaIndex,
            int firstGlobalDayIndex,
            BuildingDayConfig[] days,
            BuildingItemPlacement[] decorBefore,
            BuildingItemPlacement[] decorAfter)
        {
            _areaIndex = areaIndex;
            _firstGlobalDayIndex = firstGlobalDayIndex;
            _days = days ?? throw new ArgumentNullException(nameof(days));
            _decorBefore = decorBefore ?? throw new ArgumentNullException(nameof(decorBefore));
            _decorAfter = decorAfter ?? throw new ArgumentNullException(nameof(decorAfter));
        }
    }
}
