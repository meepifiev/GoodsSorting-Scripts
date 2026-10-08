using System;

namespace _Project.Core.Economy
{
    public interface IWalletStorage
    {
        event Action<ResourceType> Changed;

        int Get(ResourceType resource);
        void Add(ResourceType resource, int amount);
        bool TrySpend(ResourceType resource, int amount);
    }
}
