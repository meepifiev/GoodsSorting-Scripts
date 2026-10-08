using System;
using _Project.Core.Building;
using UnityEngine;

namespace _Project.Features.Building
{
    public class BuildingAreaBounds
    {
        private readonly IsoProjection _projection;

        public BuildingAreaBounds(IsoProjection projection)
        {
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public Rect Compute(BuildingAreaConfig area, float margin)
        {
            if (area == null)
            {
                throw new ArgumentNullException(nameof(area));
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
            {
                BuildingDayConfig day = area.GetDay(dayIndex);

                for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                {
                    BuildingTaskConfig task = day.GetTask(taskIndex);
                    Include(task.ItemsBefore, ref minX, ref minY, ref maxX, ref maxY);
                    Include(task.ItemsAfter, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            Include(area.DecorBefore, ref minX, ref minY, ref maxX, ref maxY);
            Include(area.DecorAfter, ref minX, ref minY, ref maxX, ref maxY);

            if (minX > maxX)
            {
                return new Rect(-margin, -margin, margin * 2f, margin * 2f);
            }

            return Rect.MinMaxRect(minX - margin, minY - margin, maxX + margin, maxY + margin);
        }

        public Rect Union(Rect first, Rect second)
        {
            return Rect.MinMaxRect(
                Mathf.Min(first.xMin, second.xMin),
                Mathf.Min(first.yMin, second.yMin),
                Mathf.Max(first.xMax, second.xMax),
                Mathf.Max(first.yMax, second.yMax));
        }

        private void Include(BuildingItemPlacement[] placements, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            foreach (BuildingItemPlacement placement in placements)
            {
                Vector2 screen = _projection.IsoToScreen(placement.IsoPosition);
                minX = Mathf.Min(minX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxX = Mathf.Max(maxX, screen.x);
                maxY = Mathf.Max(maxY, screen.y);
            }
        }
    }
}
