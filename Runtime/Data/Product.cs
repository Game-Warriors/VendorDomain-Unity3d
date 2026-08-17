using GameWarriors.VendorDomian.Enums;
using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{


    [Serializable]
    public class Product
    {
        [SerializeField]
        private string _productId;
        [SerializeField]
        private EProductType _type;
        [SerializeField]
        private float _price;

        public float Price => _price;
        public string ProductId => _productId;
        public EProductType Type => _type;

        public void SetProductId(string newId)
        {
            _productId = newId;
        }
    }
}
