using System.Collections.Generic;
using _Project.Core.Building;
using UnityEditor;

namespace _Project.Editor.Building
{
    public class BuildingAreaAssets
    {
        public List<BuildingAreaConfig> LoadAll()
        {
            List<BuildingAreaConfig> areas = new List<BuildingAreaConfig>();

            foreach (string guid in AssetDatabase.FindAssets("t:BuildingAreaConfig", new[] { BuildingAssetPaths.AreasFolder }))
            {
                BuildingAreaConfig area = AssetDatabase.LoadAssetAtPath<BuildingAreaConfig>(AssetDatabase.GUIDToAssetPath(guid));

                if (area != null)
                {
                    areas.Add(area);
                }
            }

            areas.Sort((left, right) => left.AreaIndex.CompareTo(right.AreaIndex));
            return areas;
        }
    }
}
