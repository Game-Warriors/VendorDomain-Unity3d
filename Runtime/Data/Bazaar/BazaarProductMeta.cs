using GameWarriors.VendorDomian.Abstraction;

#if BAZAAR
using Bazaar.Poolakey.Data;

namespace GameWarriors.VendorDomian.Data.Bazaar
{
    public class BazaarProductMeta : IPurchaseItemMeta
    {
        private readonly SKUDetails _metadata;

        public string Title => _metadata.title;
        public string Description => _metadata.description;
        public string LocalisedPrice => _metadata.price;
        public decimal Price { get; }
        public string CurrencyCode => "IRR";
        public SKUDetails SKUDetail => _metadata;
        public BazaarProductMeta(SKUDetails metadata, float price)
        {
            _metadata = metadata;
            Price = (decimal)price;
        }
    }
}
#endif