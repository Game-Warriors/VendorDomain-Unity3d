using GameWarriors.VendorDomian.Abstraction;

namespace GameWarriors.VendorDomian.Data
{
    public readonly struct PendingPurchaseData : IPendingPurchaseItem
    {
        public IProductItem Product { get; }
        public string TransactionId { get; }

        public PendingPurchaseData(IProductItem product, string transactionId)
        {
            Product = product;
            TransactionId = transactionId;
        }
    }
}