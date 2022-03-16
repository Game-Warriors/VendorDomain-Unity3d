using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    public enum ProductType { Consumable, NonConsumable };

    [Serializable]
    public class Product
    {
        [SerializeField]
        private string _productId;
        [SerializeField]
        private ProductType _type;
        [SerializeField]
        private float _price;

        public float Price => _price;
        public string ProductId => _productId;
        public ProductType Type => _type;

        public void SetProductId(string newId)
        {
            _productId = newId;
        }
    }
}
