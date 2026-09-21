namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IDelayPurchaseItem
    {
        IProductItem Product { get; }
        string TransactionId { get; }
    }
}
