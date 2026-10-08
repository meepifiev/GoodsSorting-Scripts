using System;

namespace _Project.Core.Building
{
    public readonly struct BuildingTaskRef : IEquatable<BuildingTaskRef>
    {
        public readonly int Day;
        public readonly int Task;

        public BuildingTaskRef(int day, int task)
        {
            Day = day;
            Task = task;
        }

        public bool Equals(BuildingTaskRef other)
        {
            return Day == other.Day && Task == other.Task;
        }

        public override bool Equals(object obj)
        {
            return obj is BuildingTaskRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Day * 1000 + Task;
        }

        public override string ToString()
        {
            return Day + "." + Task;
        }
    }
}
