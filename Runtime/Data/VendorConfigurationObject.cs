using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    public class VendorConfigurationObject : ScriptableObject
    {
        [SerializeField]
        private VendorPurchaseItem[] _products;

        public VendorConfigurationObject(VendorPurchaseItem[] products)
        {
            _products = products;
        }

        public VendorPurchaseItem[] Products => _products;

        public int ItemCounts => _products?.Length ?? 0;

        public void SetProducts(VendorPurchaseItem[] products)
        {
            _products = products;
        }

        public void FillItemDic(Dictionary<string, VendorPurchaseItem> productsTable)
        {
            int length = ItemCounts;
            for (int i = 0; i < length; ++i)
            {
                productsTable.Add(_products[i].Name, _products[i]);
            }
        }
    }
}
