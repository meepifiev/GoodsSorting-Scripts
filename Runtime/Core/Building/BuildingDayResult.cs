using System;

namespace _Project.Core.Building
{
    public readonly struct BuildingDayResult
    {
        public readonly int AreaIndex;
        public readonly int CompletedDay;
        public readonly int NextDay;
        public readonly bool AreaCompleted;
        public readonly BuildingDayReward Reward;

        public BuildingDayResult(int areaIndex, int completedDay, int nextDay, bool areaCompleted, BuildingDayReward reward)
        {
            AreaIndex = areaIndex;
            CompletedDay = completedDay;
            NextDay = nextDay;
            AreaCompleted = areaCompleted;
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
        }
    }
}
