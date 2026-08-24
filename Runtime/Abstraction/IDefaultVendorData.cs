using GameWarriors.VendorDomian.Data;
using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IDefaultVendorData
    {
        string MarketId { get; }
        bool IsValidate { get; }
        IEnumerable<IProductItem> PurchaseItems { get; }
        (float, IEnumerable<IProductCurrencyItem>) GetProducePriceAndData(string key);
        IEnumerable<IProductCurrencyItem> GetCurrencyByPurchaseId(string purchaseId);
        ISubscriptionInfo GetSubscriptionInfo(string itemName);
        void EnableProductOffState(string itemName);
        void DisableAllProductOffState();
    }
}
