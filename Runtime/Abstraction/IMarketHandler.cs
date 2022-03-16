using System;
using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public enum EMarketProvider { None = -1, Zarinpal = 1 }

    public interface IMarketHandler
    {
        public EVendorType VendorType { get; }
        string MarketId { get; }
        string MarketPackageName { get; }
        string VendorLink { get; }
        int UnconsumePurchaseCount { get; }
        bool HasValidation { get; }

        void Initialization(IServiceProvider serviceProvider);
        void OpenPage();
        void RateUs(Action<bool> rateDone);
        void FetchUnconsumePurchases();
        void ResolveLastUnconsumePurchase();
        void TryBuyProduct(string sku, string payload);
        void RefreshPruchases(string sku);
        void Dispose();
        VendorPurchaseItem GetProductByName(string id);
        VendorPurchaseItem GetProductNameById(string productId);
        void SetProdcutSalesOffState(string itemName, bool offState);
        void SetAllProdcutSalesOffState(bool state);
    }
}
