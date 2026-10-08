using System;
using UnityEngine;

namespace _Project.Core.Building
{
    [CreateAssetMenu(fileName = "BuildingBalanceConfig", menuName = "_Project/Building/Balance Config", order = 1)]
    public class BuildingBalanceConfig : ScriptableObject
    {
        [SerializeField] private int _unlockLevel = 3;
        [SerializeField] private int _gemsPerWin = 100;
        [SerializeField] private int _maxGemsPerWin = 500;
        [SerializeField] private int _levelsPerGemsStep = 20;
        [SerializeField] private int _introTaskCount = 5;
        [SerializeField] private int _introCostDivisor = 5;
        [SerializeField] private BuildingDayCosts[] _costsByGlobalDay = Array.Empty<BuildingDayCosts>();
        [SerializeField] private BuildingDayReward[] _rewardsByGlobalDay = Array.Empty<BuildingDayReward>();

        public int UnlockLevel => _unlockLevel;
        public int GemsPerWin => _gemsPerWin;
        public int MaxGemsPerWin => _maxGemsPerWin;
        public int LevelsPerGemsStep => _levelsPerGemsStep;

        public int GetGemsForWin(int level)
        {
            if (_gemsPerWin <= 0)
            {
                return 0;
            }

            int step = Mathf.Max(_levelsPerGemsStep, 1);
            int tier = Mathf.Max(level - 1, 0) / step;
            int reward = _gemsPerWin * (tier + 1);
            return Mathf.Clamp(reward, _gemsPerWin, Mathf.Max(_maxGemsPerWin, _gemsPerWin));
        }
        public int IntroTaskCount => _introTaskCount;
        public int IntroCostDivisor => _introCostDivisor;

        public int GetTaskCost(int globalDayIndex, int globalTaskIndex, BuildingTaskType type)
        {
            if (_costsByGlobalDay.Length == 0)
            {
                throw new InvalidOperationException(nameof(_costsByGlobalDay));
            }

            int clampedDay = Mathf.Clamp(globalDayIndex, 0, _costsByGlobalDay.Length - 1);
            int cost = _costsByGlobalDay[clampedDay].GetCost(type);

            if (globalTaskIndex < _introTaskCount && _introCostDivisor > 1)
            {
                cost = Mathf.Max(1, cost / _introCostDivisor);
            }

            return cost;
        }

        public BuildingDayReward GetDayReward(int globalDayIndex)
        {
            if (_rewardsByGlobalDay.Length == 0)
            {
                throw new InvalidOperationException(nameof(_rewardsByGlobalDay));
            }

            int clampedDay = Mathf.Clamp(globalDayIndex, 0, _rewardsByGlobalDay.Length - 1);
            return _rewardsByGlobalDay[clampedDay];
        }

        public void Initialize(
            int unlockLevel,
            int gemsPerWin,
            BuildingDayCosts[] costsByGlobalDay,
            BuildingDayReward[] rewardsByGlobalDay)
        {
            _unlockLevel = unlockLevel;
            _gemsPerWin = gemsPerWin;
            _costsByGlobalDay = costsByGlobalDay ?? throw new ArgumentNullException(nameof(costsByGlobalDay));
            _rewardsByGlobalDay = rewardsByGlobalDay ?? throw new ArgumentNullException(nameof(rewardsByGlobalDay));
        }
    }
}
