using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using UnityEngine;

#if GOOGLE
namespace GameWarriors.VendorDomian.Core
{
    using GameWarriors.VendorDomian.Constants;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine.Purchasing;


    public class GoogleHandler : IMarketHandler
    {
        // Apple App Store-specific product identifier for the subscription product.
        private const string kProductNameAppleSubscription = "com.unity3d.subscription.new";
        // Google Play Store-specific product identifier subscription product.
        private const string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";
        private StoreController _storeController;

        private IVendorEventHandler _vendorEventHandler;
        private Dictionary<string, VendorPurchaseItem> _productsNameTable;
        private Dictionary<string, VendorPurchaseItem> _productsSkuTable;
        private Dictionary<string, SubscriptionInfo> _subscriptionsTable;
        public IEnumerable<VendorPurchaseItem> PurchaseItems
        {
            get
            {
                foreach (VendorPurchaseItem item in _productsNameTable.Values)
                {
                    yield return item;
                }
            }
        }

        public string Id => MarketId.GOOGLE;
        public string MarketPackageName => "com.android.vending";
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public int? UnconsumePurchaseCount { get; private set; }

        public bool HasValidation => false;

        public bool IsInitialized { get; private set; }

        public void Dispose()
        {
            return;
        }

        public async void Initialization(IVendorResourceLoader resourceLoader, IServiceProvider serviceProvider)
        {
            IVendorEventHandler vendorEventHandler = serviceProvider.GetService(typeof(IVendorEventHandler)) as IVendorEventHandler;
            _vendorEventHandler = vendorEventHandler;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

            _storeController.OnStoreConnected += StoreConnected;

            //_storeController.ProcessPendingOrdersOnPurchasesFetched
            Task connectTask = _storeController.Connect();
            resourceLoader.LoadAsync(Id, resource => OnLoadDone(resource, connectTask));
        }

        private async void OnLoadDone(VendorConfigurationObject resource, Task connectTask)
        {
            _productsNameTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            _productsSkuTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            int length = resource.ItemCounts;
            for (int i = 0; i < length; ++i)
            {
                VendorPurchaseItem product = resource.Products[i];
                _productsNameTable.Add(product.Name, product);
                _productsSkuTable.Add(product.ProductId, product);
            }
            try
            {
                await connectTask;
            }
            catch (Exception e)
            {
                _vendorEventHandler.StoreInitializeFailed(e.ToString());
                return;
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                if (!string.IsNullOrEmpty(product.definition.id))
                {
                    VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventHandler.ConsumeFailed(purchaseItem, order.Info.Receipt, order.Info.TransactionID);
                }
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                if (!string.IsNullOrEmpty(product.definition.id))
                {
                    VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventHandler.ConsumeSuccess(purchaseItem, order.Info.Receipt, order.Info.TransactionID);
                }
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                if (!string.IsNullOrEmpty(product.definition.id))
                {
                    VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                    if (order.FailureReason == PurchaseFailureReason.UserCancelled)
                        _vendorEventHandler.UserCancelPurchase(purchaseItem, order.Details);
                    _vendorEventHandler.PurchasedFailed(purchaseItem, (int)order.FailureReason, order.Details);
                }
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            foreach (var item in products)
            {
                string sku = item.definition.id;
                if (_productsSkuTable.TryGetValue(sku, out VendorPurchaseItem product))
                {
                    product.SetPrice((float)item.metadata.localizedPrice);
                }
            }

            _vendorEventHandler.OnPurchaseItemsUpdate();
            _storeController.FetchPurchases();
        }

        private void OnPurchasePending(PendingOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventHandler.PurchasedSuccessful(purchaseItem, product.metadata.isoCurrencyCode,
                    (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, order.Info.Receipt, order.Info.TransactionID);
            }

            // IMPORTANT:
            // Tell the store that the purchase has been processed.
            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            UnconsumePurchaseCount = orders.PendingOrders.Count;
            _subscriptionsTable.Clear();
            foreach (var order in orders.ConfirmedOrders)
            {
                foreach (var item in order.Info.PurchasedProductInfo)
                {
                    SubscriptionInfo subscriptionInfo = item.subscriptionInfo;
                    _subscriptionsTable.Add(item.productId, subscriptionInfo);
                }
            }

            _vendorEventHandler.OnSubscriptionUpdate();
        }

        private void StoreConnected()
        {
            IsInitialized = true;
            _subscriptionsTable = new Dictionary<string, SubscriptionInfo>();
            var products = new List<ProductDefinition>();
            foreach (var item in _productsNameTable.Values)
            {
                products.Add(new ProductDefinition(item.ProductId, (ProductType)item.Type));
            }
            _storeController.FetchProducts(products);
        }

        public void RefreshPruchases(string sku)
        {
            _storeController.FetchPurchases();
        }

        public void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void RateUs(Action<bool> onRateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }


        public void FetchUnconsumePurchases()
        {
            _storeController.FetchPurchases();
        }

        public void ResolveLastUnconsumePurchase()
        {
            return;
        }

        public void TryBuyProduct(string sku, string payload)
        {
            VendorPurchaseItem purchaseItem = GetProductNameById(sku);
            if (_storeController == null || !IsInitialized)
            {
                _vendorEventHandler.PurchasedFailed(purchaseItem, 0, "store not initializaed");
                return;
            }

            Product product = _storeController.GetProductById(sku);

            if (product == null)
            {
                _vendorEventHandler.PurchasedFailed(purchaseItem, (int)PurchaseFailureReason.NotSupported, "product not found");
                return;
            }

            if (!product.availableToPurchase)
            {
                _vendorEventHandler.PurchasedFailed(purchaseItem, (int)PurchaseFailureReason.ProductUnavailable, "product not available");
                return;
            }

            _storeController.PurchaseProduct(sku);
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            if (p == PurchaseFailureReason.UserCancelled)
            {
                _vendorEventHandler.UserCancelPurchase("User Cancel");
            }
            else
            {
                _vendorEventHandler.OnError(0, $"Google Purchase Failed Item:{i.definition.id} : " + p.ToString());
            }
        }

        public VendorPurchaseItem GetProductByName(string id)
        {
            if (_productsNameTable.TryGetValue(id, out var item))
            {
                return item;
            }
            return default;
        }

        public VendorPurchaseItem GetProductNameById(string productId)
        {
            foreach (var item in _productsNameTable.Values)
            {
                if (string.Compare(item.ProductId, productId) == 0 || string.Compare(item.OffProductId, productId) == 0)
                    return item;
            }
            return default;
        }

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsNameTable.ContainsKey(itemName))
            {
                var item = _productsNameTable[itemName];
                item.SetOffState(offState);
            }
        }

        public void SetAllProdcutSalesOffState(bool state)
        {
            foreach (var item in _productsNameTable.Values)
            {
                item.SetOffState(state);
            }
        }
    }
}
#endif