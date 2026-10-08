using System;
using System.Collections.Generic;
using _Project.Core.Abilities;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.Level;
using _Project.Core.Score;
using VContainer.Unity;

namespace _Project.Features.Analytics
{
    public class GameplayAnalyticsReporter : IStartable, IDisposable
    {
        private readonly IAnalyticsService _analytics;
        private readonly ILevelService _level;
        private readonly IAbilityService _abilities;
        private readonly IStarsCounter _stars;

        public GameplayAnalyticsReporter(
            IAnalyticsService analytics,
            ILevelService level,
            IAbilityService abilities,
            IStarsCounter stars)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _stars = stars ?? throw new ArgumentNullException(nameof(stars));
        }

        public void Start()
        {
            _level.Finished += OnFinished;
            _abilities.Used += OnAbilityUsed;

            _analytics.ReportEvent(new DesignEvent(
                "level_start",
                new Dictionary<string, string> { { "level", _level.CurrentLevel.ToString() } }));
        }

        public void Dispose()
        {
            _level.Finished -= OnFinished;
            _abilities.Used -= OnAbilityUsed;
        }

        private void OnFinished(LevelFinishResult result)
        {
            if (result == LevelFinishResult.Won)
            {
                _analytics.ReportEvent(new DesignEvent(
                    "level_win",
                    new Dictionary<string, string>
                    {
                        { "level", _level.CurrentLevel.ToString() },
                        { "stars", _stars.Stars.ToString() }
                    }));
            }
            else
            {
                _analytics.ReportEvent(new DesignEvent(
                    "level_lose",
                    new Dictionary<string, string>
                    {
                        { "level", _level.CurrentLevel.ToString() },
                        { "reason", _level.LoseReason.ToString() }
                    }));
            }
        }

        private void OnAbilityUsed(AbilityType abilityType)
        {
            _analytics.ReportEvent(new DesignEvent(
                "booster_used",
                new Dictionary<string, string> { { "booster", abilityType.ToString() } }));
        }
    }
}
