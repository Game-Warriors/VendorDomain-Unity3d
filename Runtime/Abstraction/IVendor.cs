using System;

namespace GameWarriors.VendorDomian.Abstraction
{

    public interface IVendor
    {
        void ChangeDefaultMarket(string newDefault);
        void PurchaseProduct(string packName, bool hasOff);
        void OpenVendorLocation();
        void OpenRate(Action<bool> onDone);
        void CheckUnconsumePurchase();
        void ResolveLastUnconsumePurchase();
    }
}
