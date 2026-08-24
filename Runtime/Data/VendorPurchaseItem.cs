using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public class VendorPurchaseItem : IProductItem
    {
        [SerializeField]
        private string _name;
        [SerializeField]
        private string _productId;
        [SerializeField]
        private string _offProductId;
        [SerializeField]
        private float _price;
        [SerializeField]
        private VendorCurrencyItem[] _itemsData;
        [SerializeField]
        private EProductType _type;
        [SerializeField]
        private int _purchaseLimit;
        [SerializeField]
        private bool _isEnable;

        private bool _hasOff;
        public IPurchaseItemMeta ItemMeta { get; private set; }

        public string Name => _name;
        public string ProductId => _productId;
        public string OffProductId => _offProductId;
        public float Price => _price;
        public VendorCurrencyItem[] CurrenciesData => _itemsData;
        public EProductType Type => _type;
        public int ItemCounts => _itemsData?.Length ?? 0;
        public bool HasOff => _hasOff;
        public int PurchaseLimit => _purchaseLimit;
        public bool EnableState => _isEnable;

        string IProductItem.Id => _productId;

        IEnumerable<IProductCurrencyItem> IProductItem.CurrenciesData => CurrenciesData;

        public void SetOffState(bool state)
        {
            _hasOff = state;
        }

        public void SetPrice(float price)
        {
            _price = price;
        }

        public void SetMetaData(IPurchaseItemMeta meta)
        {
            ItemMeta = meta;
        }
    }
}
