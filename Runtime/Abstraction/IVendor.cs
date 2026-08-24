using System;

namespace GameWarriors.VendorDomian.Abstraction
{

    public interface IVendor
    {
        bool IsInitialized { get; }
        bool IsProductFetched { get; }
        bool IsPurchasesFetched { get; }

        void ChangeDefaultMarket(string newDefault);
        void PurchaseProduct(string packName, bool hasOff);
        bool ConsumePurchase(string transactionId);
        void OpenVendorLocation();
        void OpenRate(Action<bool> onDone);
        void RefreshProducts();
        void CheckUnconsumePurchase();
    }
}
