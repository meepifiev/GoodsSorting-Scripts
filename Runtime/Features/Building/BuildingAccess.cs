using System;
using _Project.Core.Building;
using _Project.Core.Progress;

namespace _Project.Features.Building
{
    public class BuildingAccess : IBuildingAccess
    {
        private readonly BuildingBalanceConfig _balance;
        private readonly IProgressStorage _progress;

        public BuildingAccess(BuildingBalanceConfig balance, IProgressStorage progress)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public int UnlockLevel => _balance.UnlockLevel;
        public int GemsPerWin => _balance.GemsPerWin;
        public bool IsUnlocked => _progress.Level >= _balance.UnlockLevel;

        public int GemsForLevel(int level)
        {
            return _balance.GetGemsForWin(level);
        }

        public bool GrantsGemsForLevel(int level)
        {
            return level >= _balance.UnlockLevel;
        }

        public bool ShouldVisitAfterLevel(int level)
        {
            return level == _balance.UnlockLevel;
        }
    }
}
