#if MYKET
using GameWarriors.VendorDomian.Abstraction;
using MyketPlugin;

namespace GameWarriors.VendorDomian.Data.Myket
{
    public class MyketProductMeta : IPurchaseItemMeta
    {
        private readonly MyketSkuInfo _metadata;

        public string Title => _metadata.Title;
        public string Description => _metadata.Description;
        public string LocalisedPrice => _metadata.Price;
        public decimal Price { get; }
        public string CurrencyCode => "IRR";

        public MyketProductMeta(MyketSkuInfo metadata, float price)
        {
            _metadata = metadata;
            Price = (decimal)price;
        }
    }
}
#endif