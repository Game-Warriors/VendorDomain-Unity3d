using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameWarriors.VendorDomian.Core
{
    public class VendorSystem : IDefaultVendorData, IVendor
    {
        private readonly Dictionary<string, IMarketHandler> _marketTable;
        private IMarketHandler _defaultMarket;
        private readonly IServiceProvider _serviceProvider;
        private string selectedId;


        public string MarketId => _defaultMarket?.Id;

        public bool IsValidate => _defaultMarket.HasValidation;

        [UnityEngine.Scripting.Preserve]
        public VendorSystem(IServiceProvider serviceProvider, IMarketGroup marketGroup, IVendorEventListener vendorEventHandler)
        {
            if (marketGroup == null)
                throw new ArgumentNullException("the market group is null");

            _marketTable = new Dictionary<string, IMarketHandler>(2);
            foreach (var market in marketGroup.Markets)
            {
                if (market.Id == marketGroup.DefaultMarketId)
                    _defaultMarket = market;
                _marketTable.Add(market.Id, market);
            }
            if (_defaultMarket == null)
            {
                foreach (var market in marketGroup.Markets)
                {
                    _defaultMarket = market;
                    break;
                }
            }
            _serviceProvider = serviceProvider;
        }

        [UnityEngine.Scripting.Preserve]
        public async Task WaitForLoading()
        {
            foreach (var item in _marketTable.Values)
            {
                await item.Loading();
            }
        }


        [UnityEngine.Scripting.Preserve]
        public IEnumerator WaitForLoadingCoroutine()
        {
            foreach (var item in _marketTable.Values)
            {
                yield return item.LoadingEnumerable();
            }
        }

        void IVendor.PurchaseProduct(string packName, bool hasOff)
        {
            selectedId = packName;
            VendorPurchaseItem product = _defaultMarket.GetProductByName(selectedId);
            string productId = hasOff && product.HasOff ? product.OffProductId : product.ProductId;
            _defaultMarket.TryBuyProduct(productId, Guid.NewGuid().ToString());
        }

        void IVendor.OpenVendorLocation()
        {
            _defaultMarket.OpenPage();
        }

        void IVendor.OpenRate(Action<bool> onDone)
        {
            _defaultMarket.RateUs(onDone);
        }

        void IVendor.CheckUnconsumePurchase()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return;
            _defaultMarket.FetchUnconsumePurchases();
        }

        (float, VendorCurrencyItem[]) IDefaultVendorData.GetProducePriceAndData(string key)
        {
            var item = _defaultMarket.GetProductByName(key);
            return (item.Price, item.CurrenciesData);
        }

        VendorCurrencyItem[] IDefaultVendorData.GetCurrencyByPurchaseId(string purchaseId)
        {
            var item = _defaultMarket.GetProductNameById(purchaseId);
            return item.CurrenciesData;
        }


        void IDefaultVendorData.EnableProductOffState(string itemName)
        {
            _defaultMarket.SetProdcutSalesOffState(itemName, true);
        }

        void IDefaultVendorData.DisableAllProductOffState()
        {
            _defaultMarket.SetAllProdcutSalesOffState(false);
        }

        public void ResolveLastUnconsumePurchase()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable || _marketTable == null)
            {
                //_purchaseHandler.OnItemNotPurchase();
            }
            else
            {
                _defaultMarket?.ResolveLastUnconsumePurchase();
            }
        }
    }
}