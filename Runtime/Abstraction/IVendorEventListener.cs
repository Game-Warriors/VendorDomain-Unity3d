using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorEventListener
    {
        void OnVendorStateChanged(string marketId, EStoreSetupState setupState);
        void PurchasedFailed(string marketId, IProductItem purchaseItem, int state, string error);
        void PurchasedSuccessful(string marketId, IProductItem purchaseItem, string currencyType,
            long purchaseTime, string orderId, string transactionId, EPurchaseOrigin purchaseOrigin);
        void StoreInitializeFailed(string marketId, string error);
        void UserCancelPurchase(string marketId, IProductItem purchaseItem, string error);
        void OnError(string marketId, int state, string error);
        void ConsumeSuccess(string marketId, IProductItem purchaseItem, string token, string transactionId);
        void ConsumeFailed(string marketId, IProductItem purchaseItem, string token, string transactionId);
        void OnPurchaseItemsUpdate(string marketId);
        void OnSubscriptionsUpdate(string marketId);
    }
}
