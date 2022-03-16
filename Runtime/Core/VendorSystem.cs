using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Core
{
    public class VendorSystem : IVendorData, IVendor
    {
        private readonly IMarketHandler _marketHandler;
        private readonly IVendorEventHandler _vendorEventHandler;
        private readonly IServiceProvider _serviceProvider;
        private string selectedId;

        public EVendorType VendorId => _marketHandler?.VendorType ?? EVendorType.None;

        int IVendorData.UnconsumePurchaseCount => _marketHandler?.UnconsumePurchaseCount ?? 0;

        public string MarketId => _marketHandler?.MarketId;

        public bool IsValidate => _marketHandler.HasValidation;

        [UnityEngine.Scripting.Preserve]
        public VendorSystem(IServiceProvider serviceProvider, IMarketHandler marketHandler, IVendorEventHandler vendorEventHandler)
        {
            _marketHandler = marketHandler;
            _vendorEventHandler = vendorEventHandler;
            _serviceProvider = serviceProvider;
        }

        [UnityEngine.Scripting.Preserve]
        public Task WaitForLoading()
        {
            _marketHandler.Initialization(_serviceProvider);
            return Task.CompletedTask;
        }

        void IVendor.PurchaseProduct(string packName, bool hasOff)
        {
            selectedId = packName;
            VendorPurchaseItem product = _marketHandler.GetProductByName(selectedId);
            //Debug.Log("try to buy pack id :" + packName);
            //Debug.Log("try to buy product id :" + product.ProductId);
            if (product.Type == ProductType.Consumable)
            {
                string productId = hasOff && product.HasOff ? product.OffProductId : product.ProductId;
                //Debug.Log("try to buy product id :" + productId);
                _marketHandler.TryBuyProduct(productId, Guid.NewGuid().ToString());
            }
            else
                _vendorEventHandler.PurchasedFailed(0, "Product is not consumable");
        }

        void IVendor.OpenVendorLocation()
        {
            _marketHandler.OpenPage();
        }

        void IVendor.OpenRate(Action<bool> onDone)
        {
            _marketHandler.RateUs(onDone);
        }

        void IVendor.CheckUnconsumePurchase()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return;
            _marketHandler.FetchUnconsumePurchases();
        }

        (float, VendorCurrencyItem[]) IVendorData.GetProducePriceAndData(string key)
        {
            var item = _marketHandler.GetProductByName(key);
            return (item.Price, item.CurrenciesData);
        }

        VendorCurrencyItem[] IVendorData.GetCurrencyByPurchaseId(string purchaseId)
        {
            var item = _marketHandler.GetProductNameById(purchaseId);
            return item.CurrenciesData;
        }


        void IVendorData.EnableProductOffState(string itemName)
        {
            _marketHandler.SetProdcutSalesOffState(itemName, true);
        }

        void IVendorData.DisableAllProductOffState()
        {
            _marketHandler.SetAllProdcutSalesOffState(false);
        }

        public void ResolveLastUnconsumePurchase()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable || _marketHandler == null)
            {
                //_purchaseHandler.OnItemNotPurchase();
            }
            else
            {
                _marketHandler?.ResolveLastUnconsumePurchase();
            }
        }
    }
}