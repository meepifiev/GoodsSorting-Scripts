using System;
using _Project.Core.Abilities;
using _Project.Core.Economy;
using _Project.Core.Lives;
using _Project.Core.Shop;
using PrimeGames.SDK;
using PrimeGames.SDK.Common;
using VContainer.Unity;

namespace _Project.Infrastructure.Prime
{
    public class PrimeShopService : IShopService, IStartable
    {
        private readonly ShopConfig _config;
        private readonly IWalletStorage _wallet;
        private readonly IBoosterInventory _boosters;
        private readonly ILivesService _lives;

        public PrimeShopService(ShopConfig config, IWalletStorage wallet, IBoosterInventory boosters, ILivesService lives)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _boosters = boosters ?? throw new ArgumentNullException(nameof(boosters));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
        }

        public event Action Changed;
        public event Action<string> Purchased;

        public void Start()
        {
            PrimeSDK.WaitForProviders(RestorePending);
        }

        public string GetPrice(string productTag)
        {
            if (string.IsNullOrEmpty(productTag))
            {
                return string.Empty;
            }

            try
            {
                ProductData data = PrimeSDK.Payments.GetProductData(productTag);
                return data == null ? string.Empty : data.GetFullPriceInteger();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public bool IsOwned(string productTag)
        {
            ShopProductConfig product = _config.Find(productTag);

            if (product == null || product.IsConsumable)
            {
                return false;
            }

            try
            {
                return PrimeSDK.Payments.IsAlreadyPurchased(productTag);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Purchase(string productTag)
        {
            ShopProductConfig product = _config.Find(productTag);

            if (product == null)
            {
                return;
            }

            if (product.IsConsumable == false && IsOwned(productTag))
            {
                return;
            }

            PrimeSDK.Payments.Purchase(
                productTag,
                onSuccess: () => GiveProduct(productTag, announce: true),
                onError: () => { });
        }

        private void RestorePending()
        {
            try
            {
                PrimeSDK.Payments.RestorePurchases(OnRestoreData);
            }
            catch (Exception)
            {
            }
        }

        private void OnRestoreData(IRestoreData restoreData)
        {
            if (restoreData == null)
            {
                return;
            }

            foreach (string productTag in restoreData.PendingProducts)
            {
                string tag = productTag;
                restoreData.RestoreProduct(tag, onProductRestore: () => GiveProduct(tag, announce: false));
            }
        }

        private void GiveProduct(string productTag, bool announce)
        {
            ShopProductConfig product = _config.Find(productTag);

            if (product == null)
            {
                return;
            }

            if (product.Gold > 0)
            {
                _wallet.Add(ResourceType.Gold, product.Gold);
            }

            foreach (BoosterReward booster in product.Boosters)
            {
                if (booster.Count > 0)
                {
                    _boosters.Add(booster.AbilityType, booster.Count);
                }
            }

            for (int i = 0; i < product.Lives; i++)
            {
                _lives.AddLife();
            }

            if (product.GrantInfiniteLives)
            {
                _lives.SetInfinite(true);
            }

            Changed?.Invoke();

            if (announce)
            {
                Purchased?.Invoke(productTag);
            }
        }
    }
}
