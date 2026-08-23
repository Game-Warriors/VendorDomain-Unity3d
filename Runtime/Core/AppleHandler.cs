using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using UnityEngine;

#if APPLE
namespace GameWarriors.VendorDomian.Core
{
    using GameWarriors.VendorDomian.Constants;
    using GameWarriors.VendorDomian.Enums;
    using System.Collections.Generic;
    using UnityEngine.iOS;
    using UnityEngine.Purchasing;

    public sealed class AppleHandler : IMarketHandler
    {
        private StoreController _storeController;
        private IVendorEventListener _vendorEventListener;
        private Dictionary<string, VendorPurchaseItem> _productsNameTable;
        private Dictionary<string, VendorPurchaseItem> _productsSkuTable;
        private Dictionary<string, SubscriptionInfo> _subscriptionsTable;
        private EStoreSetupState _state;
        private bool _isFetchingProducts;

        public string Id => MarketId.APPLE;
        public string MarketPackageName => "itms-apps://";
        public string VendorLink { get; private set; }
        public int? UnconsumePurchaseCount { get; private set; }
        public bool HasValidation => false;
        public bool IsLoading => _productsNameTable == null;
        public IEnumerable<VendorPurchaseItem> PurchaseItems => _productsNameTable != null
            ? _productsNameTable.Values
            : Array.Empty<VendorPurchaseItem>();
        public bool IsInitialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;

        public AppleHandler(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {
        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener
                ?? throw new InvalidOperationException($"{nameof(IVendorEventListener)} is not registered.");
            if (_storeController != null)
                return;

            _storeController = UnityIAPServices.StoreController(AppleAppStore.Name);
            _storeController.ProcessPendingOrdersOnPurchasesFetched(false);
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            _storeController.OnStoreConnected += OnStoreConnected;

            await TryConnecting();
        }

        public void Dispose()
        {
            if (_storeController == null)
                return;

            _storeController.OnPurchasePending -= OnPurchasePending;
            _storeController.OnPurchasesFetched -= OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _storeController.OnPurchaseFailed -= OnPurchaseFailed;
            _storeController.OnProductsFetched -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
            _storeController.OnStoreConnected -= OnStoreConnected;
            _storeController = null;
        }

        private async System.Threading.Tasks.Task<bool> TryConnecting()
        {
            if (_storeController == null)
                return false;

            try
            {
                SetState(EStoreSetupState.Initializing);
                await _storeController.Connect();
                return true;
            }
            catch (Exception exception)
            {
                SetState(EStoreSetupState.None);
                _vendorEventListener?.StoreInitializeFailed(Id, exception.ToString());
                return false;
            }
        }

        private void OnLoadDone(IVendorConfigurationObject resource)
        {
            if (resource == null)
            {
                _productsNameTable = new Dictionary<string, VendorPurchaseItem>();
                _productsSkuTable = new Dictionary<string, VendorPurchaseItem>();
                _vendorEventListener?.StoreInitializeFailed(Id, $"The resource for market id {Id} is null.");
                return;
            }

            VendorLink = "https://apps.apple.com/" + resource.StoreUrl;
            _productsNameTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            _productsSkuTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts * 2);
            for (int i = 0; i < resource.ItemCounts; ++i)
            {
                VendorPurchaseItem product = resource.Products[i];
                _productsNameTable[product.Name] = product;
                AddSku(product.ProductId, product);
                AddSku(product.OffProductId, product);
            }

            if (IsInitialized)
                RefreshProducts();
        }

        private void AddSku(string sku, VendorPurchaseItem product)
        {
            if (!string.IsNullOrEmpty(sku))
                _productsSkuTable[sku] = product;
        }

        private void OnStoreConnected()
        {
            _subscriptionsTable = new Dictionary<string, SubscriptionInfo>();
            SetState(EStoreSetupState.Initialized);
            if (_productsNameTable != null)
                RefreshProducts();
        }

        public void RefreshProducts()
        {
            if (_storeController == null || _productsNameTable == null || _isFetchingProducts)
                return;

            var products = new List<ProductDefinition>(_productsSkuTable.Count);
            foreach (KeyValuePair<string, VendorPurchaseItem> entry in _productsSkuTable)
                products.Add(new ProductDefinition(entry.Key, (ProductType)entry.Value.Type));

            if (products.Count == 0)
                return;

            _isFetchingProducts = true;
            _storeController.FetchProductsWithNoRetries(products);
        }

        public void RefreshPurchases(string sku)
        {
            if (_storeController == null)
                return;

            _storeController.RestoreTransactions((success, error) =>
            {
                if (!success)
                    _vendorEventListener?.OnError(Id, 0, error ?? "Apple purchase restore failed.");
            });
        }

        public void FetchUnconsumePurchases()
        {
            _storeController?.FetchPurchases();
        }

        public void ResolveLastUnconsumePurchase()
        {
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            VendorPurchaseItem purchaseItem = GetProductNameById(sku);
            if (_storeController == null)
            {
                _vendorEventListener?.PurchasedFailed(Id, purchaseItem, 0, "Store is not initialized.");
                return;
            }

            if (!IsInitialized && !await TryConnecting())
                return;

            Product product = _storeController.GetProductById(sku);
            if (product == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem,
                    (int)PurchaseFailureReason.NotSupported, "Product was not found.");
                return;
            }

            if (!product.availableToPurchase)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem,
                    (int)PurchaseFailureReason.ProductUnavailable, "Product is not available for purchase.");
                return;
            }

            _storeController.PurchaseProduct(product);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            ProcessPendingOrder(order, EPurchaseOrigin.FreshPurchase);
        }

        private void ProcessPendingOrder(PendingOrder order, EPurchaseOrigin purchaseOrigin)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.PurchasedSuccessful(Id, purchaseItem,
                    product.metadata.isoCurrencyCode, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    order.Info.Receipt, order.Info.TransactionID, purchaseOrigin);
            }

            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.ConsumeSuccess(Id, purchaseItem,
                    order.Info.Receipt, order.Info.TransactionID);
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.ConsumeFailed(Id, purchaseItem,
                    order.Info.Receipt, order.Info.TransactionID);
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                VendorPurchaseItem purchaseItem = GetProductNameById(product.definition.id);
                if (order.FailureReason == PurchaseFailureReason.UserCancelled)
                    _vendorEventListener.UserCancelPurchase(Id, purchaseItem, order.Details);

                _vendorEventListener.PurchasedFailed(Id, purchaseItem,
                    (int)order.FailureReason, order.Details);
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            _isFetchingProducts = false;
            foreach (Product item in products)
            {
                if (_productsSkuTable.TryGetValue(item.definition.id, out VendorPurchaseItem product))
                {
                    product.SetPrice((float)item.metadata.localizedPrice);
                    product.SetMetaData(new GoogeProductMeta(item.metadata));
                }
            }

            SetState(EStoreSetupState.FetchProducts);
            _vendorEventListener.OnPurchaseItemsUpdate(Id);
            _storeController.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            _isFetchingProducts = false;
            _vendorEventListener?.OnError(Id, 0, failure.ToString());
        }

        private void OnPurchasesFetched(Orders orders)
        {
            UnconsumePurchaseCount = orders.PendingOrders.Count;
            foreach (PendingOrder order in orders.PendingOrders)
                ProcessPendingOrder(order, EPurchaseOrigin.RecoveredUnconfirmedPurchase);

            _subscriptionsTable.Clear();
            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                foreach (var productInfo in order.Info.PurchasedProductInfo)
                {
                    if (productInfo.subscriptionInfo != null)
                        _subscriptionsTable[productInfo.productId] = productInfo.subscriptionInfo;
                }
            }

            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            _vendorEventListener?.OnError(Id, 0, failure.ToString());
        }

        public VendorPurchaseItem GetProductByName(string itemName)
        {
            if (_productsNameTable != null && _productsNameTable.TryGetValue(itemName, out VendorPurchaseItem item))
                return item;
            return default;
        }

        public VendorPurchaseItem GetProductNameById(string productId)
        {
            if (_productsSkuTable != null && !string.IsNullOrEmpty(productId) &&
                _productsSkuTable.TryGetValue(productId, out VendorPurchaseItem item))
                return item;
            return default;
        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string itemName)
        {
            VendorPurchaseItem item = GetProductByName(itemName);
            if (item != null && _subscriptionsTable != null &&
                _subscriptionsTable.TryGetValue(item.ProductId, out SubscriptionInfo info))
                return new SubscriptionData(info.GetExpireDate());
            return null;
        }

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsNameTable != null && _productsNameTable.TryGetValue(itemName, out VendorPurchaseItem item))
                item.SetOffState(offState);
        }

        public void SetAllProdcutSalesOffState(bool state)
        {
            if (_productsNameTable == null)
                return;
            foreach (VendorPurchaseItem item in _productsNameTable.Values)
                item.SetOffState(state);
        }

        public void OpenPage()
        {
            Application.OpenURL(VendorLink);
        }

        public void RateUs(Action<bool> onRateDone)
        {
            onRateDone?.Invoke(Device.RequestStoreReview());
        }

        private void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }
    }
}
#endif
