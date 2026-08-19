using GameWarriors.VendorDomian.Data;
using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IDefaultVendorData
    {
        string MarketId { get; }
        bool IsValidate { get; }
        IEnumerable<VendorPurchaseItem> PurchaseItems { get; }
        (float, VendorCurrencyItem[]) GetProducePriceAndData(string key);
        VendorCurrencyItem[] GetCurrencyByPurchaseId(string purchaseId);
        ISubscriptionInfo GetSubscriptionInfo(string itemName);
        void EnableProductOffState(string itemName);
        void DisableAllProductOffState();
    }
}
