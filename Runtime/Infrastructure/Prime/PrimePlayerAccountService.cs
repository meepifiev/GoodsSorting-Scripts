using System;
using _Project.Core.Leaderboard;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class PrimePlayerAccountService : IPlayerAccountService
    {
        public bool IsLoggedIn
        {
            get
            {
                try
                {
                    return PrimeSDK.Player.IsLoggedIn;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public void Login(Action onSuccess, Action onError)
        {
            try
            {
                PrimeSDK.Player.InvokeLogin(onSuccess, onError);
            }
            catch (Exception)
            {
                onError?.Invoke();
            }
        }
    }
}
