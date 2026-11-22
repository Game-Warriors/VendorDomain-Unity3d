using GameWarriors.VendorDomian.Abstraction;

namespace GameWarriors.VendorDomian.Data
{
    public readonly struct DelayPurchaseData : IDelayPurchaseItem
    {
        public IProductItem Product { get; }
        public string TransactionId { get; }

        public DelayPurchaseData(IProductItem product, string transactionId)
        {
            Product = product;
            TransactionId = transactionId;
        }
    }
}
