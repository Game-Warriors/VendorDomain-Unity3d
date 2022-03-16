using System.Collections.Generic;
using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorEventHandler
    {
        void PurchasedFailed(int state, string error);
        void PurchasedSuccessful(VendorPurchaseItem purchaseItem, string currencyType, long purchaseTime, string purchaseToken);
        void StoreInitializeFailed();
        void UserCancelPurchase(string error);
        void OnError(int state, string error);
        void ConsumeFailed(string sku, string purchaseToken);
        void UpdatePurchaseItems(IEnumerable<VendorPurchaseItem> itemIterator);
    }
}
