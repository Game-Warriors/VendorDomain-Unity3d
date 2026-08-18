using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IDefaultVendorData
    {
        string MarketId { get; }
        bool IsValidate { get; }

        (float, VendorCurrencyItem[]) GetProducePriceAndData(string key);
        VendorCurrencyItem[] GetCurrencyByPurchaseId(string purchaseId);
        void EnableProductOffState(string itemName);
        void DisableAllProductOffState();
    }
}
