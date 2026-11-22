namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IPurchaseItemMeta
    {
        string Title { get; }
        string Description { get; }
        string LocalisedPrice { get; }
        decimal Price { get; }
        string CurrencyCode { get; }
    }
}