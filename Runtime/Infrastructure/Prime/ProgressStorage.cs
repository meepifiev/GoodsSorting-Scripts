using _Project.Core.Progress;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class ProgressStorage : IProgressStorage
    {
        private const string LevelKey = "progress_level";
        private const int FirstLevel = 1;

        public int Level
        {
            get => PrimeSDK.Data.GetInt(LevelKey, FirstLevel);
            set => PrimeSDK.Data.SetInt(LevelKey, value, important: false);
        }

        public void Save()
        {
            PrimeSDK.Data.Save();
        }
    }
}
