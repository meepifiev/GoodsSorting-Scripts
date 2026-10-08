using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [Serializable]
    public class BuildingDayConfig
    {
        [SerializeField] private BuildingTaskConfig[] _tasks = Array.Empty<BuildingTaskConfig>();

        public BuildingDayConfig(BuildingTaskConfig[] tasks)
        {
            _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        }

        public int TaskCount => _tasks.Length;

        public BuildingTaskConfig GetTask(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= _tasks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(taskIndex));
            }

            return _tasks[taskIndex];
        }
    }
}
