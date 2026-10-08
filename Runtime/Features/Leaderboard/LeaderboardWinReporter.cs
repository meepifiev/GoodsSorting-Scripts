using System;
using _Project.Core.Leaderboard;
using _Project.Core.Level;
using _Project.Core.Progress;
using VContainer.Unity;

namespace _Project.Features.Leaderboard
{
    public class LeaderboardWinReporter : IInitializable, IDisposable
    {
        private readonly ILevelService _levelService;
        private readonly IWinsStorage _wins;
        private readonly ILeaderboardService _leaderboard;

        public LeaderboardWinReporter(ILevelService levelService, IWinsStorage wins, ILeaderboardService leaderboard)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _wins = wins ?? throw new ArgumentNullException(nameof(wins));
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
        }

        public void Initialize()
        {
            _levelService.Finished += OnFinished;
        }

        public void Dispose()
        {
            _levelService.Finished -= OnFinished;
        }

        private void OnFinished(LevelFinishResult result)
        {
            if (result != LevelFinishResult.Won)
            {
                return;
            }

            _wins.AddWin();
            _leaderboard.SubmitScore(_wins.Wins);
        }
    }
}
