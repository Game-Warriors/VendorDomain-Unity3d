using System.Collections.Generic;
using GameWarriors.VendorDomian.Abstraction;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    public class VendorConfigurationObject : ScriptableObject, IVendorConfigurationObject
    {
        [SerializeField]
        private string _marketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _products;

        public VendorPurchaseItem[] Products => _products;
        public string StoreUrl => _marketPackUrl;
        public int ItemCounts => _products?.Length ?? 0;

        IEnumerable<IProductItem> IVendorConfigurationObject.Products => Products;

        public void SetProducts(VendorPurchaseItem[] products)
        {
            _products = products;
        }

        public void SetMarketPackUrl(string marketPackUrl)
        {
            _marketPackUrl = marketPackUrl;
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
