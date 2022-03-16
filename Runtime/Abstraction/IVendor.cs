using System;

namespace GameWarriors.VendorDomian.Abstraction
{
    public enum EVendorType { None, Bazaar, Google, Apple, Myket, Windows }

    public interface IVendor
    {

        void PurchaseProduct(string packName, bool hasOff);
        void OpenVendorLocation();
        void OpenRate(Action<bool> onDone);
        void CheckUnconsumePurchase();
        void ResolveLastUnconsumePurchase();
    }
}
