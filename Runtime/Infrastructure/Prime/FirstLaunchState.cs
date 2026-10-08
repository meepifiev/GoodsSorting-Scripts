using _Project.Core.Progress;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class FirstLaunchState : IFirstLaunchState
    {
        private const string LaunchedKey = "has_launched";

        public bool IsFirstLaunch => PrimeSDK.Data.GetBool(LaunchedKey, false) == false;

        public void MarkLaunched()
        {
            PrimeSDK.Data.SetBool(LaunchedKey, true, important: true);
            PrimeSDK.Data.Save();
        }
    }
}
