using GameWarriors.VendorDomian.Abstraction;
using UnityEngine.Purchasing;

namespace GameWarriors.VendorDomian.Data
{
    public class GoogeProductMeta : IPurchaseItemMeta
    {
        private readonly ProductMetadata _metadata;

        public string Title => _metadata.localizedTitle;
        public string Description => _metadata.localizedDescription;
        public string LocalisedPrice => _metadata.localizedPriceString;
        public decimal Price => _metadata.localizedPrice;
        public string CurrencyCode => _metadata.isoCurrencyCode;

        public GoogeProductMeta(ProductMetadata metadata)
        {
            _metadata = metadata;
        }
    }
}
