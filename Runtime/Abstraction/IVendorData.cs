using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorData
    {
        string MarketId { get; }
        int UnconsumePurchaseCount { get; }
        bool IsValidate { get; }

        (float, VendorCurrencyItem[]) GetProducePriceAndData(string key);
        VendorCurrencyItem[] GetCurrencyByPurchaseId(string purchaseId);
        void EnableProductOffState(string itemName);
        void DisableAllProductOffState();
    }
}
