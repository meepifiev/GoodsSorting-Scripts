namespace _Project.Core.Building
{
    public interface IBuildingAccess
    {
        int UnlockLevel { get; }
        int GemsPerWin { get; }
        bool IsUnlocked { get; }

        bool GrantsGemsForLevel(int level);

        int GemsForLevel(int level);

        bool ShouldVisitAfterLevel(int level);
    }
}
