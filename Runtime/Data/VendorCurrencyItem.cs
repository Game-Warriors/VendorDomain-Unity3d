using GameWarriors.VendorDomian.Abstraction;
using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public class VendorCurrencyItem : IProductCurrencyItem
    {
        [SerializeField]
        private string _currnecyId;
        [SerializeField]
        private int _quantity;

        public string CurrnecyId => _currnecyId;

        public int Quantity => _quantity;
    }
}
