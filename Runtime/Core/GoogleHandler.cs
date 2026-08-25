using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using UnityEngine;

#if GOOGLE
namespace GameWarriors.VendorDomian.Core
{
    using GameWarriors.VendorDomian.Constants;
    using GameWarriors.VendorDomian.Enums;
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
        private bool _isFetchingProducts;
        private EStoreSetupState _state;

        private IVendorEventListener _vendorEventListener;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, SubscriptionInfo> _subscriptionsTable;
        private Dictionary<string, PendingOrder> _orderTable;

        public string Id => MarketId.GOOGLE;
        public string MarketPackageName => "com.android.vending";
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;
        public int? UnconsumePurchaseCount => _orderTable?.Count;
        public bool HasValidation => false;

        public bool IsLoading => _productsNameTable == null;
        public bool IsInitialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;

        IEnumerable<IProductItem> IMarketHandler.PurchaseItems => _productsNameTable.Values;

        public IEnumerable<IPendingPurchaseItem> PendingPurchaseItems
        {
            get
            {
                foreach (var item in _orderTable)
                {
                    string id = item.Value.Info.PurchasedProductInfo[0].productId;
                    yield return new PendingPurchaseData(_productsSkuTable[id], item.Key);
                }
            }
        }

        public GoogleHandler(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {

        }

        public void Dispose()
        {
            return;
        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventListener vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            _vendorEventListener = vendorEventListener;
            _storeController = UnityIAPServices.StoreController();
            _storeController.ProcessPendingOrdersOnPurchasesFetched(false);
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            _storeController.OnStoreConnected += StoreConnected;

            //_storeController.ProcessPendingOrdersOnPurchasesFetched
            await TryConnecting();

        }

        private async Task<bool> TryConnecting()
        {
            try
            {
                SetState(EStoreSetupState.Initializing);
                await _storeController.Connect();
            }
            catch (Exception e)
            {
                SetState(EStoreSetupState.None);
                _vendorEventListener.StoreInitializeFailed(Id, e.ToString());
                return false;
            }

            return true;
        }

        private async void OnLoadDone(IVendorConfigurationObject resource)
        {
            if (resource == null)
            {
                _productsNameTable = new();
                _productsSkuTable = new();
                throw new ArgumentNullException($"the resource for market id {Id} in null");
            }
            _productsNameTable = new(resource.ItemCounts);
            _productsSkuTable = new(resource.ItemCounts);

            foreach (IProductItem product in resource.Products)
            {
                _productsNameTable.Add(product.Name, product);
                _productsSkuTable.Add(product.Id, product);
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            _orderTable.Remove(order.Info.TransactionID, out _);
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                if (!string.IsNullOrEmpty(product.definition.id))
                {
                    IProductItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventListener.ConsumeFailed(Id, purchaseItem, order.Info.Receipt, order.Info.TransactionID);
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
                    IProductItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventListener.ConsumeSuccess(Id, purchaseItem, order.Info.Receipt, order.Info.TransactionID);
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
                    IProductItem purchaseItem = GetProductNameById(product.definition.id);
                    if (order.FailureReason == PurchaseFailureReason.ExistingPurchasePending || order.FailureReason == PurchaseFailureReason.DuplicateTransaction)
                    {
                        _storeController.FetchPurchases();
                        PendingOrder pendingOrder = GetOrderByProductId(product.definition.id);
                        if (pendingOrder != null)
                        {
                            _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, product.metadata.isoCurrencyCode,
                                (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                                order.Info.Receipt, order.Info.TransactionID, EPurchaseOrigin.FreshPurchase);
                            return;
                        }
                    }
                    else if (order.FailureReason == PurchaseFailureReason.UserCancelled)
                        _vendorEventListener.UserCancelPurchase(Id, purchaseItem, order.Details);
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)order.FailureReason, order.Details);
                }
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            ProcessPendingOrder(order, EPurchaseOrigin.FreshPurchase);
        }

        private void ProcessPendingOrder(PendingOrder order, EPurchaseOrigin purchaseOrigin)
        {
            _orderTable ??= new();
            _orderTable.TryAdd(order.Info.TransactionID, order);
            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                IProductItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, product.metadata.isoCurrencyCode,
                    (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                    order.Info.Receipt, order.Info.TransactionID, purchaseOrigin);
            }
        }

        private void OnPurchasesFetched(Orders orders)
        {
            foreach (PendingOrder order in orders.PendingOrders)
                ProcessPendingOrder(order, EPurchaseOrigin.RecoveredUnconfirmedPurchase);

            _subscriptionsTable.Clear();
            foreach (var order in orders.ConfirmedOrders)
            {
                foreach (var item in order.Info.PurchasedProductInfo)
                {
                    SubscriptionInfo subscriptionInfo = item.subscriptionInfo;
                    _subscriptionsTable.Add(item.productId, subscriptionInfo);
                }
            }

            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        private void StoreConnected()
        {
            SetState(EStoreSetupState.Initialized);
            _subscriptionsTable = new Dictionary<string, SubscriptionInfo>();
            RefreshProducts();
        }

        public void RefreshProducts()
        {
            if (_isFetchingProducts || _storeController == null || _state < EStoreSetupState.Initialized)
                return;
            var products = new List<ProductDefinition>();
            foreach (var item in _productsNameTable.Values)
            {
                products.Add(new ProductDefinition(item.Id, (ProductType)item.Type));
            }
            _isFetchingProducts = true;
            _storeController.FetchProductsWithNoRetries(products);
        }

        public void RefreshPurchases(string sku)
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


        public async void TryBuyProduct(string sku, string payload)
        {
            IProductItem purchaseItem = GetProductNameById(sku);
            if (_storeController == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, 0, "store not initializaed");
                return;
            }

            if (!IsInitialized)
            {
                bool isSuccess = await TryConnecting();
                if (!isSuccess)
                {
                    return;
                }
            }

            Product product = _storeController.GetProductById(sku);
            if (product == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)PurchaseFailureReason.NotSupported, "product not found");
                return;
            }

            if (!product.availableToPurchase)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)PurchaseFailureReason.ProductUnavailable, "product not available");
                return;
            }

            _storeController.PurchaseProduct(sku);
        }

        public bool ConsumePurchase(string transactionId)
        {
            if (_orderTable.Remove(transactionId, out var order))
            {
                _storeController.ConfirmPurchase(order);
                return true;
            }
            return false;
        }

        public IProductItem GetProductByName(string id)
        {
            if (_productsNameTable.TryGetValue(id, out var item))
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

        public ISubscriptionInfo GetSubscriptionInfoByName(string productName)
        {
            if (_productsNameTable.TryGetValue(productName, out var item))
            {
                if (_subscriptionsTable.TryGetValue(item.Id, out SubscriptionInfo info))
                    return new SubscriptionData(info.GetExpireDate());
            }
            return null;
        }

        private void OnProductsFetched(List<Product> products)
        {
            _isFetchingProducts = false;
            foreach (var item in products)
            {
                string sku = item.definition.id;
                if (_productsSkuTable.TryGetValue(sku, out IProductItem product))
                {
                    product.SetPrice((float)item.metadata.localizedPrice);
                    product.SetMetaData(new GoogleProductMeta(item.metadata));
                }
            }

            SetState(EStoreSetupState.FetchProducts);
            _storeController.FetchPurchases();
            _vendorEventListener.OnPurchaseItemsUpdate(Id);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failed)
        {
            _isFetchingProducts = false;
        }

        private void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }

        private PendingOrder GetOrderByProductId(string productId)
        {
            if (_orderTable == null)
                return null;
            foreach (var order in _orderTable.Values)
            {
                foreach (var item in order.CartOrdered.Items())
                {
                    if (item.Product.definition.id == productId)
                        return order;
                }
            }
            return null;
        }
    }
}
#endif
