namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IPendingPurchaseItem
    {
        IProductItem Product { get; }
        string TransactionId { get; }
    }
}