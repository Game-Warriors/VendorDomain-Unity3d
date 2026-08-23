using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorEventListener
    {
        void OnVendorStateChanged(string marketId, EStoreSetupState setupState);
        void PurchasedFailed(string marketId, VendorPurchaseItem purchaseItem, int state, string error);
        void PurchasedSuccessful(string marketId, VendorPurchaseItem purchaseItem, string currencyType,
            long purchaseTime, string token, string transactionId, EPurchaseOrigin purchaseOrigin);
        void StoreInitializeFailed(string marketId, string error);
        void UserCancelPurchase(string marketId, VendorPurchaseItem purchaseItem, string error);
        void OnError(string marketId, int state, string error);
        void ConsumeSuccess(string marketId, VendorPurchaseItem purchaseItem, string token, string transactionId);
        void ConsumeFailed(string marketId, VendorPurchaseItem purchaseItem, string token, string transactionId);
        void OnPurchaseItemsUpdate(string marketId);
        void OnSubscriptionsUpdate(string marketId);
    }
}
