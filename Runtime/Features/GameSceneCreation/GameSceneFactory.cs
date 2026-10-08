using System;
using _Project.Core.Level;
using _Project.Core.SceneCreation;
using _Project.Core.Time;
using _Project.Features.CellSpawner;
using _Project.Features.Legacy;
using _Project.Features.Legacy.Levels;
using Cysharp.Threading.Tasks;

namespace _Project.Features.GameSceneCreation
{
    public class GameSceneFactory : IGameSceneFactory
    {
        private readonly ILevelLoadingService<LegacyLevelConfig> _levelLoadingService;
        private readonly LevelShelfSpawner _levelShelfSpawner;
        private readonly ILevelTimer _levelTimer;

        public GameSceneFactory(
            ILevelLoadingService<LegacyLevelConfig> levelLoadingService,
            LevelShelfSpawner levelShelfSpawner,
            ILevelTimer levelTimer)
        {
            _levelLoadingService = levelLoadingService ?? throw new ArgumentNullException(nameof(levelLoadingService));
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _levelTimer = levelTimer ?? throw new ArgumentNullException(nameof(levelTimer));
        }
        
        public async UniTask Create(int level)
        {
            LegacyLevelConfig legacyLevelConfig = await _levelLoadingService.LoadAsync(level);
            _levelShelfSpawner.Spawn(legacyLevelConfig);

            _levelTimer.StartCountdown(legacyLevelConfig.level.timeToPlay);
        }
    }
}
