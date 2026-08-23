using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorConfigurationObject
    {
        VendorPurchaseItem[] Products { get; }
        string StoreUrl { get; }
        int ItemCounts { get; }
    }
}
