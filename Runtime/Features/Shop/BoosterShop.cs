using System;
using _Project.Core.Abilities;
using _Project.Core.Advertising;
using _Project.Core.Economy;
using _Project.Core.Shop;

namespace _Project.Features.Shop
{
    public class BoosterShop : IBoosterShop
    {
        private const string AdRewardId = "booster";

        private readonly BoosterShopConfig _config;
        private readonly IWalletStorage _wallet;
        private readonly IBoosterInventory _inventory;
        private readonly IAdvertisingService _advertising;

        public BoosterShop(
            BoosterShopConfig config,
            IWalletStorage wallet,
            IBoosterInventory inventory,
            IAdvertisingService advertising)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _advertising = advertising ?? throw new ArgumentNullException(nameof(advertising));
        }

        public int GetCoinPrice(AbilityType abilityType)
        {
            BoosterShopEntry entry = _config.Find(abilityType);
            return entry == null ? 0 : entry.CoinPrice;
        }

        public int GetCoinAmount(AbilityType abilityType)
        {
            BoosterShopEntry entry = _config.Find(abilityType);
            return entry == null ? 0 : entry.CoinAmount;
        }

        public int GetAdAmount(AbilityType abilityType)
        {
            BoosterShopEntry entry = _config.Find(abilityType);
            return entry == null ? 0 : entry.AdAmount;
        }

        public bool CanAffordCoins(AbilityType abilityType)
        {
            return _wallet.Get(ResourceType.Gold) >= GetCoinPrice(abilityType);
        }

        public bool BuyForCoins(AbilityType abilityType)
        {
            BoosterShopEntry entry = _config.Find(abilityType);

            if (entry == null)
            {
                return false;
            }

            if (_wallet.TrySpend(ResourceType.Gold, entry.CoinPrice) == false)
            {
                return false;
            }

            _inventory.Add(abilityType, entry.CoinAmount);
            return true;
        }

        public void BuyForAd(AbilityType abilityType, Action onGranted)
        {
            BoosterShopEntry entry = _config.Find(abilityType);

            if (entry == null)
            {
                return;
            }

            _advertising.ShowRewarded(
                AdRewardId,
                rewarded: () =>
                {
                    _inventory.Add(abilityType, entry.AdAmount);
                    onGranted?.Invoke();
                },
                closed: null);
        }
    }
}
