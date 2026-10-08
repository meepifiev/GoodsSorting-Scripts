using System;
using System.Collections.Generic;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.Economy;
using _Project.Core.Lives;
using _Project.Core.Purchasing;
using VContainer.Unity;

namespace _Project.Features.Analytics
{
    public class EconomyAnalyticsReporter : IStartable, IDisposable
    {
        private readonly IAnalyticsService _analytics;
        private readonly IWalletStorage _wallet;
        private readonly ILivesService _lives;
        private readonly IPurchaseService _purchases;

        private readonly Dictionary<ResourceType, int> _lastBalance = new Dictionary<ResourceType, int>();

        private bool _wasOutOfLives;
        private bool _adsRemovedReported;

        public EconomyAnalyticsReporter(
            IAnalyticsService analytics,
            IWalletStorage wallet,
            ILivesService lives,
            IPurchaseService purchases)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
        }

        public void Start()
        {
            _wallet.Changed += OnWalletChanged;
            _lives.Changed += OnLivesChanged;
            _purchases.Changed += OnPurchasesChanged;
        }

        public void Dispose()
        {
            _wallet.Changed -= OnWalletChanged;
            _lives.Changed -= OnLivesChanged;
            _purchases.Changed -= OnPurchasesChanged;
        }

        private void OnWalletChanged(ResourceType resource)
        {
            int current = _wallet.Get(resource);

            if (_lastBalance.TryGetValue(resource, out int previous) == false)
            {
                _lastBalance[resource] = current;
                return;
            }

            _lastBalance[resource] = current;

            int delta = current - previous;

            if (delta == 0)
            {
                return;
            }

            string eventName = delta > 0 ? "currency_earned" : "currency_spent";

            _analytics.ReportEvent(new DesignEvent(
                eventName,
                new Dictionary<string, string>
                {
                    { "resource", resource.ToString() },
                    { "amount", Math.Abs(delta).ToString() },
                    { "balance", current.ToString() }
                }));
        }

        private void OnLivesChanged()
        {
            bool outOfLives = _lives.IsInfinite == false && _lives.Current <= 0;

            if (outOfLives == _wasOutOfLives)
            {
                return;
            }

            _wasOutOfLives = outOfLives;

            if (outOfLives)
            {
                _analytics.ReportEvent(new DesignEvent("out_of_lives"));
            }
        }

        private void OnPurchasesChanged()
        {
            if (_adsRemovedReported || _purchases.AdsDisabled == false)
            {
                return;
            }

            _adsRemovedReported = true;
            _analytics.ReportEvent(new DesignEvent("remove_ads_bought"));
        }
    }
}
