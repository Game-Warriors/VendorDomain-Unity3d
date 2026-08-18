using System;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public class PaymentBillItem<T>
    {
        [SerializeField]
        private string _marketId;
        [SerializeField]
        private string _purchaseId;
        [SerializeField]
        private string _purchaseToken;
        [SerializeField]
        private long _purchaseDate;
        [SerializeField]
        private float _price;
        [SerializeField]
        private string _unit;
        [SerializeField]
        public T _metaData;

        public string PurchaseId => _purchaseId;
        public string MarketId => _marketId;
        public long PurchaseDate => _purchaseDate;
        public float Price => _price;
        public string Unit => _unit;
        public T MetaData => _metaData;
        public string PurchaseToken => _purchaseToken;

        public PaymentBillItem(string marketId, string purchaseId, string purchaseToken, long purchaseDate, float price, string unit, T meta)
        {
            _marketId = marketId;
            _purchaseId = purchaseId;
            _purchaseToken = purchaseToken;
            _purchaseDate = purchaseDate;
            _price = price;
            _unit = unit;
            _metaData = meta;
        }
    }
}
