using System;
using _Project.Core.Abilities;

namespace _Project.Core.Shop
{
    public interface IBoosterShop
    {
        int GetCoinPrice(AbilityType abilityType);
        int GetCoinAmount(AbilityType abilityType);
        int GetAdAmount(AbilityType abilityType);
        bool CanAffordCoins(AbilityType abilityType);
        bool BuyForCoins(AbilityType abilityType);
        void BuyForAd(AbilityType abilityType, Action onGranted);
    }
}
