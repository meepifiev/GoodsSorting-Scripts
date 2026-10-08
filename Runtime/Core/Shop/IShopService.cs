using System;

namespace _Project.Core.Shop
{
    public interface IShopService
    {
        event Action Changed;
        event Action<string> Purchased;

        string GetPrice(string productTag);
        bool IsOwned(string productTag);
        void Purchase(string productTag);
    }
}
