using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Core.Building;
using _Project.Core.Economy;

namespace _Project.Features.Building
{
    public class BuildingService : IBuildingService
    {
        private const int NoArea = -1;

        private readonly BuildingWorldConfig _world;
        private readonly BuildingBalanceConfig _balance;
        private readonly IBuildingProgressStorage _storage;
        private readonly IWalletStorage _wallet;
        private readonly IBoosterInventory _boosters;
        private readonly Dictionary<int, BuildingAreaProgress> _progressByArea = new Dictionary<int, BuildingAreaProgress>();
        private readonly List<BuildingTaskRef> _currentDayTasks = new List<BuildingTaskRef>();

        private int _currentAreaIndex = NoArea;

        public BuildingService(
            BuildingWorldConfig world,
            BuildingBalanceConfig balance,
            IBuildingProgressStorage storage,
            IWalletStorage wallet,
            IBoosterInventory boosters)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _boosters = boosters ?? throw new ArgumentNullException(nameof(boosters));
        }

        public event Action Changed;
        public event Action<BuildingTaskRef> TaskBuilt;
        public event Action<BuildingDayResult> DayCompleted;

        public int AreaCount => _world.AreaCount;

        public int CurrentAreaIndex
        {
            get
            {
                if (_currentAreaIndex == NoArea)
                {
                    _currentAreaIndex = FindCurrentArea();
                }

                return _currentAreaIndex;
            }
        }

        public BuildingAreaConfig Area => _world.GetArea(CurrentAreaIndex);
        public int CurrentDay => CurrentProgress.CurrentDay;
        public bool IsCurrentAreaCompleted => CurrentProgress.Completed;

        public bool IsAllCompleted
        {
            get
            {
                for (int i = 0; i < _world.AreaCount; i++)
                {
                    if (_world.GetArea(i).HasTasks && GetProgress(i).Completed == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int AvailableTaskCount
        {
            get
            {
                int count = 0;

                foreach (BuildingTaskRef task in GetCurrentDayTasks())
                {
                    if (GetState(task) == BuildingTaskState.Available)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private BuildingAreaProgress CurrentProgress => GetProgress(CurrentAreaIndex);

        public BuildingAreaConfig GetArea(int areaIndex)
        {
            return _world.GetArea(areaIndex);
        }

        public bool IsAreaCompleted(int areaIndex)
        {
            return GetProgress(areaIndex).Completed;
        }

        public bool IsAreaUnlocked(int areaIndex)
        {
            return _world.GetArea(areaIndex).HasTasks && areaIndex <= CurrentAreaIndex;
        }

        public bool IsTaskDone(int areaIndex, BuildingTaskRef task)
        {
            return GetProgress(areaIndex).IsDone(task);
        }

        public int GetTheme(int areaIndex, BuildingTaskRef task)
        {
            return GetProgress(areaIndex).GetTheme(task);
        }

        public BuildingTaskConfig GetConfig(BuildingTaskRef task)
        {
            return Area.GetDay(task.Day).GetTask(task.Task);
        }

        public BuildingTaskState GetState(BuildingTaskRef task)
        {
            BuildingAreaProgress progress = CurrentProgress;

            if (progress.IsDone(task))
            {
                return BuildingTaskState.Done;
            }

            if (progress.Completed || task.Day != progress.CurrentDay)
            {
                return BuildingTaskState.Locked;
            }

            BuildingTaskConfig config = GetConfig(task);

            if (config.HasDependency && progress.IsDone(new BuildingTaskRef(task.Day, config.DependsOnTaskIndex)) == false)
            {
                return BuildingTaskState.Locked;
            }

            return BuildingTaskState.Available;
        }

        public int GetTheme(BuildingTaskRef task)
        {
            return CurrentProgress.GetTheme(task);
        }

        public int GetCost(BuildingTaskRef task)
        {
            return _balance.GetTaskCost(Area.FirstGlobalDayIndex + task.Day, GetGlobalTaskIndex(task), GetConfig(task).Type);
        }

        private int GetGlobalTaskIndex(BuildingTaskRef task)
        {
            int index = 0;

            for (int area = 0; area < CurrentAreaIndex; area++)
            {
                index += CountTasks(_world.GetArea(area));
            }

            BuildingAreaConfig current = Area;

            for (int day = 0; day < task.Day; day++)
            {
                index += current.GetDay(day).TaskCount;
            }

            return index + task.Task;
        }

        private int CountTasks(BuildingAreaConfig area)
        {
            int count = 0;

            for (int day = 0; day < area.DayCount; day++)
            {
                count += area.GetDay(day).TaskCount;
            }

            return count;
        }

        public bool CanAfford(BuildingTaskRef task)
        {
            return _wallet.Get(ResourceType.Gems) >= GetCost(task);
        }

        public bool TryBuild(BuildingTaskRef task, int theme)
        {
            if (GetState(task) != BuildingTaskState.Available)
            {
                return false;
            }

            if (_wallet.TrySpend(ResourceType.Gems, GetCost(task)) == false)
            {
                return false;
            }

            int areaIndex = CurrentAreaIndex;
            BuildingAreaProgress progress = GetProgress(areaIndex);
            progress.MarkDone(task, theme);
            Save(areaIndex, progress);
            TaskBuilt?.Invoke(task);

            if (IsDayComplete(areaIndex, task.Day))
            {
                CompleteDay(areaIndex, task.Day);
            }

            Changed?.Invoke();
            return true;
        }

        public IReadOnlyList<BuildingTaskRef> GetCurrentDayTasks()
        {
            _currentDayTasks.Clear();
            BuildingAreaProgress progress = CurrentProgress;
            BuildingAreaConfig area = Area;

            if (progress.Completed || progress.CurrentDay >= area.DayCount)
            {
                return _currentDayTasks;
            }

            BuildingDayConfig day = area.GetDay(progress.CurrentDay);

            for (int i = 0; i < day.TaskCount; i++)
            {
                _currentDayTasks.Add(new BuildingTaskRef(progress.CurrentDay, i));
            }

            return _currentDayTasks;
        }

        private BuildingAreaProgress GetProgress(int areaIndex)
        {
            if (_progressByArea.TryGetValue(areaIndex, out BuildingAreaProgress progress) == false)
            {
                progress = _storage.Load(_world.GetArea(areaIndex).AreaIndex);
                _progressByArea[areaIndex] = progress;
            }

            return progress;
        }

        private void Save(int areaIndex, BuildingAreaProgress progress)
        {
            _storage.Save(_world.GetArea(areaIndex).AreaIndex, progress);
        }

        private int FindCurrentArea()
        {
            int lastWithTasks = 0;

            for (int i = 0; i < _world.AreaCount; i++)
            {
                if (_world.GetArea(i).HasTasks == false)
                {
                    continue;
                }

                lastWithTasks = i;

                if (GetProgress(i).Completed == false)
                {
                    return i;
                }
            }

            return lastWithTasks;
        }

        private bool IsDayComplete(int areaIndex, int dayIndex)
        {
            BuildingDayConfig day = _world.GetArea(areaIndex).GetDay(dayIndex);
            BuildingAreaProgress progress = GetProgress(areaIndex);

            for (int i = 0; i < day.TaskCount; i++)
            {
                if (progress.IsDone(new BuildingTaskRef(dayIndex, i)) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteDay(int areaIndex, int dayIndex)
        {
            BuildingAreaConfig area = _world.GetArea(areaIndex);
            BuildingAreaProgress progress = GetProgress(areaIndex);
            BuildingDayReward reward = _balance.GetDayReward(area.FirstGlobalDayIndex + dayIndex);
            GrantReward(reward);

            int nextDay = dayIndex + 1;
            bool areaCompleted = nextDay >= area.DayCount;

            if (areaCompleted)
            {
                progress.MarkCompleted();
            }
            else
            {
                progress.StartDay(nextDay);
            }

            Save(areaIndex, progress);

            if (areaCompleted)
            {
                _currentAreaIndex = FindCurrentArea();
            }

            DayCompleted?.Invoke(new BuildingDayResult(areaIndex, dayIndex, nextDay, areaCompleted, reward));
        }

        private void GrantReward(BuildingDayReward reward)
        {
            if (reward.Gold > 0)
            {
                _wallet.Add(ResourceType.Gold, reward.Gold);
            }

            AddBooster(AbilityType.Crash, reward.Hammer);
            AddBooster(AbilityType.Swap, reward.Swap);
            AddBooster(AbilityType.Replace, reward.Replace);
            AddBooster(AbilityType.Freeze, reward.Freeze);
        }

        private void AddBooster(AbilityType type, int amount)
        {
            if (amount > 0)
            {
                _boosters.Add(type, amount);
            }
        }
    }
}
