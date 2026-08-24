using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorEventHandler
    {
        void OnVendorStateChanged(string id, EStoreSetupState setupState);
        void PurchasedFailed(string id, VendorPurchaseItem purchaseItem, int state, string error);
        void PurchasedSuccessful(string id, VendorPurchaseItem purchaseItem, string currencyType, long purchaseTime, string token, string transactionId);
        void StoreInitializeFailed(string id, string error);
        void UserCancelPurchase(string id, VendorPurchaseItem purchaseItem, string error);
        void OnError(string id, int state, string error);
        void ConsumeSuccess(string id, VendorPurchaseItem purchaseItem, string token, string transactionId);
        void ConsumeFailed(string id, VendorPurchaseItem purchaseItem, string token, string transactionId);
        void OnPurchaseItemsUpdate(string id);
        void OnSubscriptionUpdate(string id);
    }
}
