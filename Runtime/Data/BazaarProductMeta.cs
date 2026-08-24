using Bazaar.Poolakey.Data;
using GameWarriors.VendorDomian.Abstraction;

namespace GameWarriors.VendorDomian.Data
{
#if BAZAAR
    public class BazaarProductMeta : IPurchaseItemMeta
    {
        private readonly SKUDetails _metadata;

        public string Title => _metadata.title;
        public string Description => _metadata.description;
        public string LocalisedPrice => _metadata.price;
        public decimal Price { get; }
        public string CurrencyCode => "IRR";

        public BazaarProductMeta(SKUDetails metadata, float price)
        {
            _metadata = metadata;
            Price = (decimal)price;
        }
    }
#endif
}