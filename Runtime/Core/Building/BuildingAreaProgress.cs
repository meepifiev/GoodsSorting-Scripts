using System;
using System.Collections.Generic;

namespace _Project.Core.Building
{
    public class BuildingAreaProgress
    {
        private readonly Dictionary<BuildingTaskRef, int> _themesByDoneTask = new Dictionary<BuildingTaskRef, int>();

        public BuildingAreaProgress()
        {
        }

        public BuildingAreaProgress(int currentDay, bool completed, IEnumerable<KeyValuePair<BuildingTaskRef, int>> doneTasks)
        {
            if (doneTasks == null)
            {
                throw new ArgumentNullException(nameof(doneTasks));
            }

            CurrentDay = currentDay;
            Completed = completed;

            foreach (KeyValuePair<BuildingTaskRef, int> pair in doneTasks)
            {
                _themesByDoneTask[pair.Key] = pair.Value;
            }
        }

        public int CurrentDay { get; private set; }
        public bool Completed { get; private set; }
        public IReadOnlyDictionary<BuildingTaskRef, int> DoneTasks => _themesByDoneTask;

        public bool IsDone(BuildingTaskRef task)
        {
            return _themesByDoneTask.ContainsKey(task);
        }

        public int GetTheme(BuildingTaskRef task)
        {
            return _themesByDoneTask.TryGetValue(task, out int theme) ? theme : 0;
        }

        public void MarkDone(BuildingTaskRef task, int theme)
        {
            _themesByDoneTask[task] = theme;
        }

        public void StartDay(int day)
        {
            if (day < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(day));
            }

            CurrentDay = day;
        }

        public void MarkCompleted()
        {
            Completed = true;
        }
    }
}
