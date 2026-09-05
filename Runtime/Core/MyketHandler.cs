using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace GameWarriors.VendorDomian.Core
{
#if MYKET
    using GameWarriors.VendorDomian.Data.Myket;
    using MyketPlugin;
    using static MyketPlugin.MyketPurchase;

    public class MyketHandler : IMarketHandler
    {
        private string _key;
        private bool _isFetchingProducts;
        private bool _isFetchingPurchases;
        private EStoreSetupState _state;

        private IVendorEventListener _vendorEventListener;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, MyketPurchase> _subscriptionsTable;
        private Dictionary<string, MyketPurchase> _orderTable;

        public bool IsLoading => _productsNameTable == null;
        public bool NotInitialize => _state == EStoreSetupState.None;
        public bool Initialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;

        public string Id => MarketId.MYKET;

        public string MarketPackageName => throw new NotSupportedException();

        public string VendorLink => $"https://myket.ir/app/{Application.identifier}";

        public int? UnconsumePurchaseCount => _orderTable?.Count;

        public bool HasValidation => false;

        IEnumerable<IProductItem> IMarketHandler.PurchaseItems => _productsNameTable.Values;

        public IEnumerable<IPendingPurchaseItem> PendingPurchaseItems
        {
            get
            {
                foreach (var item in _orderTable)
                {
                    string id = item.Value.ProductId;
                    yield return new PendingPurchaseData(_productsSkuTable[id], item.Key);
                }
            }
        }
        public MyketHandler(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {

        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            SetState(EStoreSetupState.Initializing);
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            IABEventManager.billingSupportedEvent += billingSupportedEvent;
            IABEventManager.billingNotSupportedEvent += billingNotSupportedEvent;
            IABEventManager.queryInventorySucceededEvent += queryInventorySucceededEvent;
            IABEventManager.queryInventoryFailedEvent += queryInventoryFailedEvent;
            IABEventManager.querySkuDetailsSucceededEvent += querySkuDetailsSucceededEvent;
            IABEventManager.querySkuDetailsFailedEvent += querySkuDetailsFailedEvent;
            IABEventManager.queryPurchasesSucceededEvent += queryPurchasesSucceededEvent;
            IABEventManager.queryPurchasesFailedEvent += queryPurchasesFailedEvent;
            IABEventManager.purchaseSucceededEvent += purchaseSucceededEvent;
            IABEventManager.purchaseFailedEvent += purchaseFailedEvent;
            IABEventManager.consumePurchaseSucceededEvent += consumePurchaseSucceededEvent;
            IABEventManager.consumePurchaseFailedEvent += consumePurchaseFailedEvent;
            MyketIAB.init(_key);
        }

        private void queryPurchasesFailedEvent(string obj)
        {
            _isFetchingPurchases = false;
            _vendorEventListener.OnError(Id, 9, obj);
        }

        private void queryPurchasesSucceededEvent(List<MyketPurchase> purchases)
        {
            _isFetchingPurchases = false;
            _orderTable ??= new Dictionary<string, MyketPurchase>();
            _orderTable.Clear();
            _subscriptionsTable.Clear();
            foreach (MyketPurchase item in purchases)
            {
                if (_productsSkuTable.TryGetValue(item.ProductId, out IProductItem product))
                {
                    if (item.PurchaseState == MyketPurchaseState.Purchased
                        && product.Type == EProductType.Subscription)
                    {
                        if (DateTimeOffset.FromUnixTimeMilliseconds(item.PurchaseTime).AddDays(product.PurchaseLimit) > DateTime.UtcNow)
                            _subscriptionsTable.Add(item.ProductId, item);
                    }
                    else if (product.Type == EProductType.Consumable)
                    {
                        _orderTable.Add(item.PurchaseToken, item);
                    }
                }
            }
        }

        private void querySkuDetailsFailedEvent(string obj)
        {
            _isFetchingProducts = false;
            _vendorEventListener.OnError(Id, (int)_state, obj);
        }

        private void querySkuDetailsSucceededEvent(List<MyketSkuInfo> skuInfo)
        {
            _isFetchingProducts = false;
            foreach (MyketSkuInfo item in skuInfo)
            {
                string sku = item.ProductId;
                if (_productsSkuTable.TryGetValue(sku, out IProductItem product))
                {
                    if (float.TryParse(item.Price, out var floatPrice))
                        product.SetPrice(floatPrice);
                    product.SetMetaData(new MyketProductMeta(item, floatPrice));
                }
            }
        }

        private void queryInventorySucceededEvent(List<MyketPurchase> purchases, List<MyketSkuInfo> skuInfo)
        {
            _isFetchingProducts = false;
            foreach (MyketSkuInfo item in skuInfo)
            {
                string sku = item.ProductId;
                if (_productsSkuTable.TryGetValue(sku, out IProductItem product))
                {
                    if (float.TryParse(item.Price, out var floatPrice))
                        product.SetPrice(floatPrice);
                    product.SetMetaData(new MyketProductMeta(item, floatPrice));
                }
            }
            SetState(EStoreSetupState.FetchProducts);
            queryPurchasesSucceededEvent(purchases);
            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        private void queryInventoryFailedEvent(string obj)
        {
            _isFetchingPurchases = false;
            _isFetchingProducts = false;
            _vendorEventListener.OnError(Id, (int)_state, obj);
        }



        private void billingNotSupportedEvent(string obj)
        {
            SetState(EStoreSetupState.None);
            _vendorEventListener?.StoreInitializeFailed(Id, obj);
        }

        private void billingSupportedEvent()
        {
            SetState(EStoreSetupState.Initialized);
        }

        private void OnLoadDone(IVendorConfigurationObject resource)
        {
            if (resource == null)
            {
                _productsNameTable = new();
                _productsSkuTable = new();
                throw new ArgumentNullException($"the resource for market id {Id} in null");
            }
            _key = resource.StoreKey;
            _productsNameTable = new(resource.ItemCounts);
            _productsSkuTable = new(resource.ItemCounts);
            _subscriptionsTable = new();

            foreach (IProductItem product in resource.Products)
            {
                _productsNameTable.Add(product.Name, product);
                _productsSkuTable.Add(product.Id, product);
            }

            if (Initialized)
                RefreshProducts();
        }

        public bool ConsumePurchase(string transactionId)
        {
            if (_orderTable.TryGetValue(transactionId, out var order))
            {
                MyketIAB.consumeProduct(order.ProductId);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            // Remove all event handlers
            IABEventManager.billingSupportedEvent -= billingSupportedEvent;
            IABEventManager.billingNotSupportedEvent -= billingNotSupportedEvent;
            IABEventManager.queryInventorySucceededEvent -= queryInventorySucceededEvent;
            IABEventManager.queryInventoryFailedEvent -= queryInventoryFailedEvent;
            IABEventManager.querySkuDetailsSucceededEvent -= querySkuDetailsSucceededEvent;
            IABEventManager.querySkuDetailsFailedEvent -= querySkuDetailsFailedEvent;
            IABEventManager.queryPurchasesSucceededEvent -= queryPurchasesSucceededEvent;
            IABEventManager.queryPurchasesFailedEvent -= queryPurchasesFailedEvent;
            IABEventManager.purchaseSucceededEvent -= purchaseSucceededEvent;
            IABEventManager.purchaseFailedEvent -= purchaseFailedEvent;
            IABEventManager.consumePurchaseSucceededEvent -= consumePurchaseSucceededEvent;
            IABEventManager.consumePurchaseFailedEvent -= consumePurchaseFailedEvent;
            MyketIAB.unbindService();
        }

        private void consumePurchaseFailedEvent(string obj)
        {
            _vendorEventListener.PurchasedFailed(Id, default, 10, obj);
        }

        private void consumePurchaseSucceededEvent(MyketPurchase purchase)
        {
            if (_orderTable.Remove(purchase.PurchaseToken))
            {

            }
        }

        private void purchaseFailedEvent(string obj)
        {
            _vendorEventListener.PurchasedFailed(Id, default, 9, obj);
        }

        private void purchaseSucceededEvent(MyketPurchase purchase)
        {
            IProductItem purchaseItem = GetProductNameById(purchase.ProductId);
            _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, "IRR",
                                  purchase.PurchaseTime,
                                  purchase.OrderId, purchase.PurchaseToken, EPurchaseOrigin.FreshPurchase);
        }

        public void FetchUnconsumePurchases()
        {
            RefreshPurchases(string.Empty);
        }

        public IProductItem GetProductByName(string itemName)
        {
            if (_productsNameTable.TryGetValue(Id, out var item))
            {
                return item;
            }
            return default;
        }

        public IProductItem GetProductNameById(string productId)
        {
            foreach (var item in _productsNameTable.Values)
            {
                if (string.Compare(item.Id, productId) == 0 || string.Compare(item.OffProductId, productId) == 0)
                    return item;
            }
            return default;
        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string productName)
        {
            if (_productsNameTable.TryGetValue(productName, out var product))
            {
                if (_subscriptionsTable.TryGetValue(product.Id, out MyketPurchase info))
                    return new SubscriptionData(DateTimeOffset.FromUnixTimeMilliseconds(info.PurchaseTime).AddDays(product.PurchaseLimit).DateTime);
            }
            return null;
        }

        public void OpenPage()
        {
            Application.OpenURL($"https://myket.ir/app/{Application.identifier}");
        }

        public void RateUs(Action<bool> rateDone)
        {
            throw new NotSupportedException();
        }

        public void RefreshProducts()
        {
            if (_isFetchingProducts || _isFetchingPurchases || _productsNameTable == null || _state < EStoreSetupState.Initialized)
                return;

            int length = _productsNameTable.Count;
            var products = new string[length];
            int counter = 0;
            foreach (var item in _productsNameTable.Values)
            {
                products[counter] = item.Id;
                ++counter;
            }
            _isFetchingProducts = true;
            _isFetchingPurchases = true;
            MyketIAB.queryInventory(products);
        }

        public void RefreshPurchases(string sku)
        {
            if (_isFetchingProducts)
                return;
            if (_state == EStoreSetupState.Initializing)
                return;
            if (_state == EStoreSetupState.None)
            {
                MyketIAB.init(_key);
                return;
            }
            _isFetchingProducts = true;
            MyketIAB.queryPurchases();
        }

        public void SetAllProdcutSalesOffState(bool state)
        {
            foreach (var item in _productsNameTable.Values)
            {
                item.SetOffState(state);
            }
        }

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsNameTable.ContainsKey(itemName))
            {
                var item = _productsNameTable[itemName];
                item.SetOffState(offState);
            }
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            IProductItem purchaseItem = GetProductNameById(sku);

            if (NotInitialize)
            {
                MyketIAB.init(_key);
                await Task.Delay(1000);
            }

            if (NotInitialize)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, 0, "store not initializaed");
                return;
            }
            MyketIAB.purchaseProduct(sku, payload);
        }

        private void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }
    }
#endif
}
