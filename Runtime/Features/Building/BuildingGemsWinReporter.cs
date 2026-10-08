using System;
using _Project.Core.Building;
using _Project.Core.Economy;
using _Project.Core.Level;
using VContainer.Unity;

namespace _Project.Features.Building
{
    public class BuildingGemsWinReporter : IInitializable, IDisposable
    {
        private readonly ILevelService _levelService;
        private readonly IBuildingAccess _access;
        private readonly IWalletStorage _wallet;

        public BuildingGemsWinReporter(ILevelService levelService, IBuildingAccess access, IWalletStorage wallet)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            _access = access ?? throw new ArgumentNullException(nameof(access));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
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
            if (result != LevelFinishResult.Won || _access.GrantsGemsForLevel(_levelService.CurrentLevel) == false)
            {
                return;
            }

            _wallet.Add(ResourceType.Gems, _access.GemsForLevel(_levelService.CurrentLevel));
        }
    }
}
