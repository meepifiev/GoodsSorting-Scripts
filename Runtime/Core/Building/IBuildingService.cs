using System;
using System.Collections.Generic;

namespace _Project.Core.Building
{
    public interface IBuildingService
    {
        event Action Changed;
        event Action<BuildingTaskRef> TaskBuilt;
        event Action<BuildingDayResult> DayCompleted;

        int AreaCount { get; }
        int CurrentAreaIndex { get; }
        BuildingAreaConfig Area { get; }
        int CurrentDay { get; }
        bool IsCurrentAreaCompleted { get; }
        bool IsAllCompleted { get; }
        int AvailableTaskCount { get; }

        BuildingAreaConfig GetArea(int areaIndex);
        bool IsAreaCompleted(int areaIndex);
        bool IsAreaUnlocked(int areaIndex);
        bool IsTaskDone(int areaIndex, BuildingTaskRef task);
        int GetTheme(int areaIndex, BuildingTaskRef task);

        BuildingTaskConfig GetConfig(BuildingTaskRef task);
        BuildingTaskState GetState(BuildingTaskRef task);
        int GetTheme(BuildingTaskRef task);
        int GetCost(BuildingTaskRef task);
        bool CanAfford(BuildingTaskRef task);
        bool TryBuild(BuildingTaskRef task, int theme);
        IReadOnlyList<BuildingTaskRef> GetCurrentDayTasks();
    }
}
