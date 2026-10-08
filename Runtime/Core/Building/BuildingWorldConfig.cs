using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [CreateAssetMenu(fileName = "BuildingWorldConfig", menuName = "_Project/Building/World Config", order = 3)]
    public class BuildingWorldConfig : ScriptableObject
    {
        [SerializeField] private BuildingAreaConfig[] _areas = Array.Empty<BuildingAreaConfig>();

        public int AreaCount => _areas.Length;

        public BuildingAreaConfig GetArea(int areaIndex)
        {
            if (areaIndex < 0 || areaIndex >= _areas.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(areaIndex));
            }

            return _areas[areaIndex];
        }

        public void Initialize(BuildingAreaConfig[] areas)
        {
            _areas = areas ?? throw new ArgumentNullException(nameof(areas));
        }
    }
}
