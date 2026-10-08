using System;
using System.Collections.Generic;

namespace _Project.Core.Leaderboard
{
    public interface ILeaderboardService
    {
        void GetTop(int count, Action<IReadOnlyList<LeaderboardEntry>> onLoaded);
        void SubmitScore(int score);
    }
}
