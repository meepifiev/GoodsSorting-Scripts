using System;
using _Project.Core.Progress;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class WinsStorage : IWinsStorage
    {
        private const string WinsKey = "wins_total";

        public event Action Changed;

        public int Wins
        {
            get
            {
                try
                {
                    return PrimeSDK.Data.GetInt(WinsKey, 0);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public void AddWin()
        {
            try
            {
                int value = Wins + 1;
                PrimeSDK.Data.SetInt(WinsKey, value, important: true);
                PrimeSDK.Data.Save();
            }
            catch (Exception)
            {
            }

            Changed?.Invoke();
        }
    }
}
