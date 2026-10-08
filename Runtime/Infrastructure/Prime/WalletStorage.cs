using System;
using _Project.Core.Economy;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class WalletStorage : IWalletStorage
    {
        private const string KeyPrefix = "currency_";
        private const int DefaultGold = 100;

        public event Action<ResourceType> Changed;

        public int Get(ResourceType resource)
        {
            return PrimeSDK.Data.GetInt(KeyFor(resource), resource == ResourceType.Gold ? DefaultGold : 0);
        }

        public void Add(ResourceType resource, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Set(resource, Get(resource) + amount);
        }

        public bool TrySpend(ResourceType resource, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            int current = Get(resource);

            if (current < amount)
            {
                return false;
            }

            Set(resource, current - amount);
            return true;
        }

        private void Set(ResourceType resource, int value)
        {
            PrimeSDK.Data.SetInt(KeyFor(resource), value, important: false);
            PrimeSDK.Data.Save();
            OnChanged(resource);
        }

        private string KeyFor(ResourceType resource)
        {
            return KeyPrefix + resource.ToString().ToLowerInvariant();
        }

        private void OnChanged(ResourceType resource)
        {
            Changed?.Invoke(resource);
        }
    }
}
