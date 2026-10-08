using System;
using System.Collections.Generic;
using _Project.Core.Building;
using UnityEditor;

namespace _Project.Editor.Building
{
    public class BuildingDependencyResolver
    {
        private const int NoDependency = -1;

        private readonly BuildingImportReport _report;

        public BuildingDependencyResolver(BuildingImportReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public int ToSameDayIndex(ReferenceAreaData area, int firstGlobalTaskIndex, int dayIndex, int taskIndex, int globalDependency)
        {
            if (globalDependency < 0)
            {
                return NoDependency;
            }

            int localFlat = globalDependency - firstGlobalTaskIndex;
            int dayStart = 0;

            for (int day = 0; day < area.Days.Count; day++)
            {
                int count = area.Days[day].Tasks.Count;

                if (localFlat >= dayStart && localFlat < dayStart + count)
                {
                    return Resolve(dayIndex, taskIndex, day, localFlat - dayStart, globalDependency);
                }

                dayStart += count;
            }

            _report.Warn("Task d" + dayIndex + ".t" + taskIndex + " depends on global task " + globalDependency + " outside this area; dependency dropped");
            return NoDependency;
        }

        public void FixExistingConfigs(List<BuildingAreaConfig> areas)
        {
            if (areas == null)
            {
                throw new ArgumentNullException(nameof(areas));
            }

            foreach (BuildingAreaConfig area in areas)
            {
                Dictionary<int, BuildingTaskRef> byGlobalIndex = new Dictionary<int, BuildingTaskRef>();

                for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
                {
                    BuildingDayConfig day = area.GetDay(dayIndex);

                    for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                    {
                        if (TryReadGlobalIndex(day.GetTask(taskIndex).TitleKey, out int globalIndex))
                        {
                            byGlobalIndex[globalIndex] = new BuildingTaskRef(dayIndex, taskIndex);
                        }
                    }
                }

                int changed = 0;

                for (int dayIndex = 0; dayIndex < area.DayCount; dayIndex++)
                {
                    BuildingDayConfig day = area.GetDay(dayIndex);

                    for (int taskIndex = 0; taskIndex < day.TaskCount; taskIndex++)
                    {
                        BuildingTaskConfig task = day.GetTask(taskIndex);

                        if (task.HasDependency == false)
                        {
                            continue;
                        }

                        int resolved = NoDependency;

                        if (byGlobalIndex.TryGetValue(task.DependsOnTaskIndex, out BuildingTaskRef target))
                        {
                            resolved = Resolve(dayIndex, taskIndex, target.Day, target.Task, task.DependsOnTaskIndex);
                        }
                        else
                        {
                            _report.Warn(area.name + " d" + dayIndex + ".t" + taskIndex + " depends on global task " + task.DependsOnTaskIndex + " outside this area; dependency dropped");
                        }

                        if (resolved != task.DependsOnTaskIndex)
                        {
                            task.SetDependsOnTaskIndex(resolved);
                            changed++;
                        }
                    }
                }

                if (changed > 0)
                {
                    EditorUtility.SetDirty(area);
                }

                _report.Count(area.name + ": dependencies rewritten", changed);
            }

            AssetDatabase.SaveAssets();
        }

        private int Resolve(int dayIndex, int taskIndex, int targetDay, int targetTask, int globalDependency)
        {
            if (targetDay < dayIndex)
            {
                return NoDependency;
            }

            if (targetDay == dayIndex && targetTask != taskIndex)
            {
                return targetTask;
            }

            _report.Warn("Task d" + dayIndex + ".t" + taskIndex + " depends on global task " + globalDependency + " (d" + targetDay + ".t" + targetTask + ") in a later day or itself; dependency dropped");
            return NoDependency;
        }

        private bool TryReadGlobalIndex(string titleKey, out int globalIndex)
        {
            globalIndex = NoDependency;

            if (string.IsNullOrEmpty(titleKey) || titleKey.StartsWith(BuildingAssetPaths.TaskTitleKeyPrefix) == false)
            {
                return false;
            }

            return int.TryParse(titleKey.Substring(BuildingAssetPaths.TaskTitleKeyPrefix.Length), out globalIndex);
        }
    }
}
