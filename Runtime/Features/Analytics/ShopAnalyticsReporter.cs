using System;
using System.Collections.Generic;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.Shop;
using VContainer.Unity;

namespace _Project.Features.Analytics
{
    public class ShopAnalyticsReporter : IStartable, IDisposable
    {
        private readonly IAnalyticsService _analytics;
        private readonly IShopService _shop;

        public ShopAnalyticsReporter(IAnalyticsService analytics, IShopService shop)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
        }

        public void Start()
        {
            _shop.Purchased += OnPurchased;
        }

        public void Dispose()
        {
            _shop.Purchased -= OnPurchased;
        }

        private void OnPurchased(string productTag)
        {
            _analytics.ReportEvent(new DesignEvent(
                "purchase",
                new Dictionary<string, string> { { "product", productTag } }));
        }
    }
}
