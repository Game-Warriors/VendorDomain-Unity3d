using GameWarriors.VendorDomian.Enums;
using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IProductItem
    {
        string Name { get; }
        string Id { get; }
        string OffProductId { get; }
        float Price { get; }
        IEnumerable<IProductCurrencyItem> CurrenciesData { get; }
        EProductType Type { get; }
        int ItemCounts { get; }
        int PurchaseLimit { get; }
        bool EnableState { get; }
        bool HasOff { get; }

        void SetOffState(bool state);
        void SetPrice(float price);
        void SetMetaData(IPurchaseItemMeta meta);
    }
}