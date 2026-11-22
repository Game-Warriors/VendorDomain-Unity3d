using GameWarriors.VendorDomian.Abstraction;


namespace GameWarriors.VendorDomian.Data
{
#if GOOGLE || APPLE || XSOLLA
    using UnityEngine.Purchasing;
    public class UnityProductMeta : IPurchaseItemMeta
    {
        private readonly ProductMetadata _metadata;

        public string Title => _metadata.localizedTitle;
        public string Description => _metadata.localizedDescription;
        public string LocalisedPrice => _metadata.localizedPriceString;
        public decimal Price => _metadata.localizedPrice;
        public string CurrencyCode => _metadata.isoCurrencyCode;

        public UnityProductMeta(ProductMetadata metadata)
        {
            _metadata = metadata;
        }
    }
#endif
}
