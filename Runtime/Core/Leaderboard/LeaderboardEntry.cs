namespace _Project.Core.Leaderboard
{
    public readonly struct LeaderboardEntry
    {
        public readonly int Position;
        public readonly string DisplayName;
        public readonly int Score;
        public readonly string AvatarUrl;

        public LeaderboardEntry(int position, string displayName, int score, string avatarUrl)
        {
            Position = position;
            DisplayName = displayName;
            Score = score;
            AvatarUrl = avatarUrl;
        }
    }
}
