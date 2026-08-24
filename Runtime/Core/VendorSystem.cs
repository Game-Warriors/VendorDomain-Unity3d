using GameWarriors.VendorDomian.Abstraction;
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

        IEnumerable<IProductItem> IDefaultVendorData.PurchaseItems => _defaultMarket.PurchaseItems;

        public bool IsInitialized => _defaultMarket.IsInitialized;

        public bool IsProductFetched => _defaultMarket.IsProductFetched;

        public bool IsPurchasesFetched => _defaultMarket.IsPurchasesFetched;



        [UnityEngine.Scripting.Preserve]
        public VendorSystem(IServiceProvider serviceProvider, IMarketGroup marketGroup)
        {
            if (marketGroup == null)
                throw new ArgumentNullException("the market group is null");

            _marketTable = new Dictionary<string, IMarketHandler>(2);
            foreach (var market in marketGroup.Markets)
            {
                if (market.Id == marketGroup.InitialDefaultMarketId)
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
            IVendorResourceLoader resourceLoader = _serviceProvider.GetService(typeof(IVendorResourceLoader)) as IVendorResourceLoader;
            foreach (var item in _marketTable.Values)
            {
                item.StartLoading(resourceLoader);
            }

            while (true)
            {
                bool anyLoading = false;
                foreach (var item in _marketTable.Values)
                {
                    if (item.IsLoading)
                    {
                        anyLoading = true;
                        break;
                    }
                }

                if (!anyLoading)
                    break;
                await Task.Delay(100);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public IEnumerator WaitForLoadingCoroutine()
        {
            IVendorResourceLoader resourceLoader = _serviceProvider.GetService(typeof(IVendorResourceLoader)) as IVendorResourceLoader;
            foreach (var item in _marketTable.Values)
            {
                item.StartLoading(resourceLoader);
            }

            while (true)
            {
                bool anyLoading = false;
                foreach (var item in _marketTable.Values)
                {
                    if (item.IsLoading)
                    {
                        anyLoading = true;
                        break;
                    }
                }

                if (!anyLoading)
                    yield break;

                yield return null;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void Initialization()
        {
            foreach (var item in _marketTable.Values)
            {
                item.Initialization(_serviceProvider);
            }
        }

        void IVendor.ChangeDefaultMarket(string newDefault)
        {
            foreach (var market in _marketTable.Values)
            {
                if (string.Equals(market.Id, newDefault, StringComparison.OrdinalIgnoreCase))
                    _defaultMarket = market;
                return;
            }
            throw new KeyNotFoundException($"the request {newDefault} market not exist");
        }

        void IVendor.PurchaseProduct(string packName, bool hasOff)
        {
            selectedId = packName;
            IProductItem product = _defaultMarket.GetProductByName(packName);
            string productId = hasOff && product.HasOff ? product.OffProductId : product.Id;
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


        void IVendor.RefreshProducts()
        {
            _defaultMarket?.RefreshProducts();
        }

        (float, IEnumerable<IProductCurrencyItem>) IDefaultVendorData.GetProducePriceAndData(string key)
        {
            var item = _defaultMarket.GetProductByName(key);
            return (item.Price, item.CurrenciesData);
        }

        IEnumerable<IProductCurrencyItem> IDefaultVendorData.GetCurrencyByPurchaseId(string purchaseId)
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

        ISubscriptionInfo IDefaultVendorData.GetSubscriptionInfo(string itemName)
        {
            return _defaultMarket?.GetSubscriptionInfoByName(itemName);
        }

        public bool ConsumePurchase(string transactionId)
        {
            return _defaultMarket.ConsumePurchase(transactionId);
        }
    }
}