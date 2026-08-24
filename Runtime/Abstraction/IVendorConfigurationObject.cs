using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorConfigurationObject
    {
        IEnumerable<IProductItem> Products { get; }
        string StoreUrl { get; }
        int ItemCounts { get; }
        string StoreKey { get; }
    }
}
