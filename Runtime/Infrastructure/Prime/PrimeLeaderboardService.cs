using System;
using System.Collections.Generic;
using _Project.Core.Leaderboard;
using PrimeGames.SDK;
using PrimeGames.SDK.Common;

namespace _Project.Infrastructure.Prime
{
    public class PrimeLeaderboardService : ILeaderboardService
    {
        private readonly LeaderboardConfig _config;

        public PrimeLeaderboardService(LeaderboardConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void GetTop(int count, Action<IReadOnlyList<LeaderboardEntry>> onLoaded)
        {
            try
            {
                PrimeSDK.Achievements.GetLeaderboard(_config.BoardId, leaderboard => OnLeaderboard(leaderboard, count, onLoaded));
            }
            catch (Exception)
            {
                onLoaded?.Invoke(Array.Empty<LeaderboardEntry>());
            }
        }

        public void SubmitScore(int score)
        {
            try
            {
                PrimeSDK.Achievements.SetScore(_config.BoardId, score);
            }
            catch (Exception)
            {
            }
        }

        private void OnLeaderboard(Leaderboard leaderboard, int count, Action<IReadOnlyList<LeaderboardEntry>> onLoaded)
        {
            PlayerScore[] players = leaderboard.players ?? Array.Empty<PlayerScore>();
            int limit = Math.Min(count, players.Length);
            List<LeaderboardEntry> result = new List<LeaderboardEntry>(limit);

            for (int i = 0; i < limit; i++)
            {
                PlayerScore player = players[i];
                result.Add(new LeaderboardEntry(player.position, player.displayName, player.score, player.profilePictureUrl));
            }

            onLoaded?.Invoke(result);
        }
    }
}
