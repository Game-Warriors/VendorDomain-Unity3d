using System;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public class PaymentBillItem<T>
    {
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
        private EVendorType _vendorType;
        [SerializeField]
        public T _metaData;

        public string PurchaseId => _purchaseId;
        public long PurchaseDate => _purchaseDate;
        public float Price => _price;
        public string Unit => _unit;
        public EVendorType VendorType => _vendorType;
        public T MetaData => _metaData;
        public string PurchaseToken => _purchaseToken;

        public PaymentBillItem(string purchaseId, string purchaseToken, long purchaseDate, float price, string unit, EVendorType vendorType, T meta)
        {
            _purchaseId = purchaseId;
            _purchaseToken = purchaseToken;
            _purchaseDate = purchaseDate;
            _price = price;
            _unit = unit;
            _vendorType = vendorType;
            _metaData = meta;
        }
    }
}
