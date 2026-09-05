using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameWarriors.VendorDomian.Abstraction;
using UnityEngine;

namespace GameWarriors.VendorDomian.Core
{
    public class SingleProviderVendorSystem : IDefaultVendorData, IVendor
    {
        private readonly IMarketHandler _defaultMarket;
        private readonly IServiceProvider _serviceProvider;

        public string MarketId => _defaultMarket?.Id;
        public bool IsValidate => _defaultMarket.HasValidation;

        IEnumerable<IProductItem> IDefaultVendorData.PurchaseItems => _defaultMarket.PurchaseItems;

        public bool IsInitialized => _defaultMarket.Initialized;

        public bool IsProductFetched => _defaultMarket.IsProductFetched;

        public bool IsPurchasesFetched => _defaultMarket.IsPurchasesFetched;



        [UnityEngine.Scripting.Preserve]
        public SingleProviderVendorSystem(IServiceProvider serviceProvider, IMarketHandler marketHandler)
        {
            _defaultMarket = marketHandler ?? throw new ArgumentNullException("the market handler is null");
            _serviceProvider = serviceProvider;
        }

        [UnityEngine.Scripting.Preserve]
        public async Task WaitForLoading()
        {
            IVendorResourceLoader resourceLoader = _serviceProvider.GetService(typeof(IVendorResourceLoader)) as IVendorResourceLoader;
            _defaultMarket.StartLoading(resourceLoader);
            while (_defaultMarket.IsLoading)
            {
                await Task.Delay(100);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public IEnumerator WaitForLoadingCoroutine()
        {
            IVendorResourceLoader resourceLoader = _serviceProvider.GetService(typeof(IVendorResourceLoader)) as IVendorResourceLoader;
            _defaultMarket.StartLoading(resourceLoader);
            while (_defaultMarket.IsLoading)
            {
                yield return null;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void Initialization()
        {
            _defaultMarket.Initialization(_serviceProvider);
        }

        void IVendor.ChangeDefaultMarket(string newDefault)
        {
            throw new NotSupportedException();
        }

        void IVendor.PurchaseProduct(string packName, bool hasOff)
        {
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