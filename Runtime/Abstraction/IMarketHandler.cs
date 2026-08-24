using System;
using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public enum EMarketProvider { None = -1, Zarinpal = 1 }

    public interface IMarketHandler
    {
        bool IsInitialized { get; }
        bool IsLoading { get; }
        string Id { get; }
        string MarketPackageName { get; }
        string VendorLink { get; }
        int? UnconsumePurchaseCount { get; }
        bool HasValidation { get; }
        IEnumerable<IProductItem> PurchaseItems { get; }
        IEnumerable<IPendingPurchaseItem> PendingPurchaseItems { get; }
        bool IsProductFetched { get; }
        bool IsPurchasesFetched { get; }

        void Initialization(IServiceProvider serviceProvider);
        void StartLoading(IVendorResourceLoader resourceLoader);
        void OpenPage();
        void RateUs(Action<bool> rateDone);
        void FetchUnconsumePurchases();
        void ConsumePurchase(string transactionId);
        void TryBuyProduct(string sku, string payload);
        void RefreshProducts();
        void RefreshPurchases(string sku);
        void Dispose();
        IProductItem GetProductByName(string itemName);
        IProductItem GetProductNameById(string productId);
        ISubscriptionInfo GetSubscriptionInfoByName(string itemName);
        void SetProdcutSalesOffState(string itemName, bool offState);
        void SetAllProdcutSalesOffState(bool state);
    }
}
