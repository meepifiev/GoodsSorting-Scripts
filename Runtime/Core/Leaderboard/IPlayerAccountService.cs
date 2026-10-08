using System;

namespace _Project.Core.Leaderboard
{
    public interface IPlayerAccountService
    {
        bool IsLoggedIn { get; }

        void Login(Action onSuccess, Action onError);
    }
}
