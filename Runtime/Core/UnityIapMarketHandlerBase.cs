using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;

// The "|| true" mirrors the guard used by XsollaHandler so this base type is
// always available wherever one of the Unity IAP based handlers is compiled.
#if GOOGLE || APPLE || XSOLLA
namespace GameWarriors.VendorDomian.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine.Purchasing;

    /// <summary>
    /// Shared implementation of <see cref="IMarketHandler"/> for every market that is
    /// driven by the Unity IAP <see cref="StoreController"/> pipeline.
    /// Concrete markets only supply the store controller, the store specific links and,
    /// when needed, the way purchases are refreshed and the configuration fields they consume.
    /// </summary>
    public abstract class UnityIapMarketHandlerBase
    {
        protected StoreController _storeController;
        protected IVendorEventListener _vendorEventListener;
        protected bool _isFetchingProducts;
        protected bool _isFetchingPurchases;

        private readonly HashSet<string> _confirmingTransactions = new();
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, SubscriptionInfo> _subscriptionsTable;
        private Dictionary<string, PendingOrder> _orderTable;
        private Dictionary<string, DeferredOrder> _deferredOrderTable;
        private EStoreSetupState _state;

        public abstract string Id { get; }
        public abstract string MarketPackageName { get; }
        public abstract string VendorLink { get; }

        public virtual bool HasValidation => false;
        public int? UnconsumePurchaseCount => _orderTable?.Count;
        public bool IsLoading => _productsNameTable == null;
        public bool Initialized => _state > EStoreSetupState.Initializing;
        public bool NotInitialize => _state == EStoreSetupState.None;
        public bool IsProductFetched => _state > EStoreSetupState.Initialized;
        public bool IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;

        public IEnumerable<IProductItem> PurchaseItems => _productsNameTable != null
            ? _productsNameTable.Values
            : Array.Empty<IProductItem>();

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

        public IEnumerable<IDelayPurchaseItem> DelayPurchaseItems
        {
            get
            {
                if (_deferredOrderTable == null)
                    yield break;

                foreach (var item in _deferredOrderTable)
                {
                    CartItem cartItem = item.Value.CartOrdered.Items()[0];
                    IProductItem product = GetProductNameById(cartItem.Product.definition.id);
                    if (product != null)
                        yield return new DelayPurchaseData(product, item.Value.Info.TransactionID);

                }
            }
        }

        protected UnityIapMarketHandlerBase(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        /// <summary>
        /// Creates the market specific <see cref="StoreController"/> instance.
        /// </summary>
        protected abstract StoreController CreateStoreController();

        /// <summary>
        /// Gives the concrete market a chance to read its own fields from the loaded
        /// configuration before the product tables are filled.
        /// </summary>
        protected virtual void OnCatalogLoaded(IVendorConfigurationObject resource)
        {
        }

        /// <summary>
        /// Performs the market specific purchases refresh. Called while the purchases
        /// fetching flag is already raised.
        /// </summary>
        protected virtual void RequestPurchasesRefresh()
        {
            _storeController.FetchPurchases();
        }

        public virtual void StartLoading(IVendorResourceLoader resourceLoader)
        {
        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener
                ?? throw new InvalidOperationException($"{nameof(IVendorEventListener)} is not registered.");
            if (_storeController != null)
                return;

            _storeController = CreateStoreController();
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
            _storeController.OnStoreDisconnected += OnStoreDisconnected;

            await TryConnecting();
        }

        public virtual void Dispose()
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
            _storeController.OnStoreDisconnected -= OnStoreDisconnected;
            _storeController = null;
            _confirmingTransactions.Clear();
            _deferredOrderTable?.Clear();
        }

        protected async Task<bool> TryConnecting()
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
                _productsNameTable = new();
                _productsSkuTable = new();
                _deferredOrderTable = new();
                _vendorEventListener?.StoreInitializeFailed(Id, $"The resource for market id {Id} is null.");
                return;
            }

            OnCatalogLoaded(resource);
            _productsNameTable = new(resource.ItemCounts);
            _productsSkuTable = new(resource.ItemCounts);
            _deferredOrderTable = new();
            foreach (IProductItem product in resource.Products)
            {
                _productsNameTable.Add(product.Name, product);
                _productsSkuTable.Add(product.Id, product);
                if (!string.IsNullOrEmpty(product.OffProductId))
                    _productsSkuTable.Add(product.OffProductId, product);
            }

            if (Initialized)
                RefreshProducts();
        }

        private void OnStoreConnected()
        {
            _subscriptionsTable = new Dictionary<string, SubscriptionInfo>();
            SetState(EStoreSetupState.Initialized);
            _vendorEventListener?.StoreInitializeSuccess(Id);
            RefreshProducts();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            SetState(EStoreSetupState.None);
            _vendorEventListener.OnError(Id, -1, description.Message);
        }

        public void RefreshProducts()
        {
            if (_isFetchingProducts || _storeController == null)
                return;
            if (_state == EStoreSetupState.Initializing)
                return;
            if (_state == EStoreSetupState.None)
            {
                _ = TryConnecting();
                return;
            }

            var products = new List<ProductDefinition>(_productsNameTable.Count);
            foreach (IProductItem item in _productsNameTable.Values)
            {
                products.Add(new ProductDefinition(item.Id, (ProductType)item.Type));
            }
            if (products.Count == 0)
                return;

            _isFetchingProducts = true;
            _storeController.FetchProductsWithNoRetries(products);
        }

        public void RefreshPurchases(string sku)
        {
            if (_storeController == null || _isFetchingPurchases)
                return;
            if (_state == EStoreSetupState.Initializing)
                return;
            if (_state == EStoreSetupState.None)
            {
                _ = TryConnecting();
                return;
            }

            BeginPurchasesRefresh();
        }

        public void FetchUnconsumePurchases()
        {
            RefreshPurchases(string.Empty);
        }

        protected void BeginPurchasesRefresh()
        {
            if (_storeController == null || _isFetchingPurchases)
                return;

            _isFetchingPurchases = true;
            RequestPurchasesRefresh();
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            IProductItem purchaseItem = GetProductNameById(sku);
            if (_storeController == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, 0, "store not initializaed");
                return;
            }

            if (NotInitialize)
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
            if (_storeController == null || _orderTable == null ||
                !_orderTable.TryGetValue(transactionId, out PendingOrder order) ||
                !_confirmingTransactions.Add(transactionId))
                return false;

            _storeController.ConfirmPurchase(order);
            return true;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            ProcessPendingOrder(order, EPurchaseOrigin.FreshPurchase);
        }

        private void ProcessPendingOrder(PendingOrder order, EPurchaseOrigin purchaseOrigin)
        {
            _orderTable ??= new();
            _orderTable.TryAdd(order.Info.TransactionID, order);
            bool isDeferred = RemoveDeferredOrders(order);
            if (isDeferred && purchaseOrigin == EPurchaseOrigin.FreshPurchase)
                purchaseOrigin = EPurchaseOrigin.DelayPurchase;

            //foreach (CartItem item in order.CartOrdered.Items())
            CartItem item = order.CartOrdered.Items()[0];

            Product product = item.Product;
            IProductItem purchaseItem = GetProductNameById(product.definition.id);
            _vendorEventListener.PurchasedSuccessful(Id, purchaseItem,
                product.metadata.isoCurrencyCode, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                order.Info.Receipt, order.Info.TransactionID, purchaseOrigin);

        }

        private void OnPurchaseConfirmed(Order order)
        {
            _confirmingTransactions.Remove(order.Info.TransactionID);

            if (order is FailedOrder)
            {
                foreach (CartItem item in order.CartOrdered.Items())
                {
                    Product product = item.Product;
                    IProductItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventListener.ConsumeFailed(Id, purchaseItem,
                        order.Info.Receipt, order.Info.TransactionID);
                }
                return;
            }

            if (order is not ConfirmedOrder)
                return;

            _orderTable?.Remove(order.Info.TransactionID);
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                IProductItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.ConsumeSuccess(Id, purchaseItem,
                    order.Info.Receipt, order.Info.TransactionID);
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            ProcessDeferredOrder(order, EPurchaseOrigin.FreshPurchase);
        }

        private void ProcessDeferredOrder(DeferredOrder order, EPurchaseOrigin purchaseOrigin)
        {
            _deferredOrderTable ??= new();
            _deferredOrderTable[GetDeferredOrderKey(order)] = order;
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                IProductItem purchaseItem = GetProductNameById(product.definition.id);
                _vendorEventListener.PurchasedDelayed(Id, purchaseItem, product.metadata.isoCurrencyCode,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), order.Info.Receipt,
                    order.Info.TransactionID, purchaseOrigin);
            }
        }

        /// <summary>
        /// Deferred orders may not own a transaction id yet (e.g. Google Play pending payments),
        /// so the first product id of the cart is used as a fallback key.
        /// </summary>
        private static string GetDeferredOrderKey(DeferredOrder order)
        {
            CartItem item = order.CartOrdered.Items()[0];
            return item.Product.definition.id;
        }

        /// <summary>
        /// Mirrors Unity IAP: once a deferred order is approved it arrives as a pending order,
        /// so every deferred order sharing a transaction id or a product with it is dropped.
        /// </summary>
        private bool RemoveDeferredOrders(Order pendingOrder)
        {
            if (_deferredOrderTable == null || _deferredOrderTable.Count == 0)
                return false;

            string productId = pendingOrder.CartOrdered.Items()[0].Product.definition.id;
            if (_deferredOrderTable.TryGetValue(productId, out var deferredOrder) && deferredOrder.Info.TransactionID == pendingOrder.Info.TransactionID)
            {
                return _deferredOrderTable.Remove(productId);
            }
            return false;
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            _isFetchingPurchases = false;
            RemoveDeferredOrders(order);
            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;
                if (string.IsNullOrEmpty(product.definition.id))
                    continue;

                IProductItem purchaseItem = GetProductNameById(product.definition.id);
                if (order.FailureReason == PurchaseFailureReason.ExistingPurchasePending ||
                    order.FailureReason == PurchaseFailureReason.DuplicateTransaction)
                {
                    BeginPurchasesRefresh();
                    PendingOrder pendingOrder = GetOrderByProductId(product.definition.id);
                    if (pendingOrder != null)
                    {
                        _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, product.metadata.isoCurrencyCode,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), pendingOrder.Info.Receipt,
                            pendingOrder.Info.TransactionID, EPurchaseOrigin.FreshPurchase);
                        return;
                    }
                }
                else if (order.FailureReason == PurchaseFailureReason.UserCancelled)
                    _vendorEventListener.UserCancelPurchase(Id, purchaseItem, (int)order.FailureReason, order.Details);
                else
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)order.FailureReason, order.Details);
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            _isFetchingProducts = false;
            foreach (Product item in products)
            {
                if (_productsSkuTable.TryGetValue(item.definition.id, out IProductItem product))
                {
                    product.SetPrice((float)item.metadata.localizedPrice);
                    product.SetMetaData(new UnityProductMeta(item.metadata));
                }
            }

            SetState(EStoreSetupState.FetchProducts);
            _vendorEventListener.OnProductItemsUpdate(Id);
            BeginPurchasesRefresh();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            _isFetchingProducts = false;
            _vendorEventListener?.OnError(Id, 0, failure.ToString());
        }

        private void OnPurchasesFetched(Orders orders)
        {
            _isFetchingPurchases = false;

            // The fetched deferred list is authoritative: drop the ones that were declined or
            // cancelled outside the app and only notify about the ones not reported before.
            Dictionary<string, DeferredOrder> previousDeferredOrders = _deferredOrderTable;
            _deferredOrderTable.Clear();
            foreach (DeferredOrder order in orders.DeferredOrders)
            {
                string key = GetDeferredOrderKey(order);
                if (previousDeferredOrders != null && previousDeferredOrders.ContainsKey(key))
                    _deferredOrderTable[key] = order;
                else
                    ProcessDeferredOrder(order, EPurchaseOrigin.RecoveredUnconfirmedPurchase);
            }

            foreach (PendingOrder order in orders.PendingOrders)
            {
                if (_orderTable.TryAdd(order.Info.TransactionID, order))
                {
                    CartItem item = order.CartOrdered.Items()[0];
                    Product product = item.Product;
                    IProductItem purchaseItem = GetProductNameById(product.definition.id);
                    _vendorEventListener.OnPendingPurchaseRecovered(Id, purchaseItem, order.Info.TransactionID);
                }
            }

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
            _isFetchingPurchases = false;
            _vendorEventListener.OnError(Id, (int)failure.FailureReason, failure.Message);
        }

        public void ResolvePendingPurchase(string transactionId)
        {
            if (_orderTable.TryGetValue(transactionId, out var order))
            {
                ProcessPendingOrder(order, EPurchaseOrigin.RecoveredUnconfirmedPurchase);
            }
        }

        public IProductItem GetProductByName(string itemName)
        {
            if (_productsNameTable != null && _productsNameTable.TryGetValue(itemName, out IProductItem item))
                return item;
            return default;
        }

        public IProductItem GetProductNameById(string productId)
        {
            if (_productsSkuTable != null && !string.IsNullOrEmpty(productId) &&
                _productsSkuTable.TryGetValue(productId, out IProductItem item))
                return item;
            return default;
        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string itemName)
        {
            IProductItem item = GetProductByName(itemName);
            if (item != null && _subscriptionsTable != null &&
                _subscriptionsTable.TryGetValue(item.Id, out SubscriptionInfo info))
                return new SubscriptionData(info.GetExpireDate());
            return null;
        }

        public IDelayPurchaseItem GetDelayPurchaseItemByName(string itemName)
        {
            IProductItem item = GetProductByName(itemName);
            if (item == null || _deferredOrderTable == null)
                return null;

            foreach (DeferredOrder order in _deferredOrderTable.Values)
            {
                CartItem cartItem = order.CartOrdered.Items()[0];
                string productId = cartItem.Product.definition.id;
                if (productId == item.Id || (!string.IsNullOrEmpty(item.OffProductId) && productId == item.OffProductId))
                    return new DelayPurchaseData(item, order.Info.TransactionID);
            }
            return null;
        }

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsNameTable != null && _productsNameTable.TryGetValue(itemName, out IProductItem item))
                item.SetOffState(offState);
        }

        public void SetAllProdcutSalesOffState(bool state)
        {
            if (_productsNameTable == null)
                return;
            foreach (IProductItem item in _productsNameTable.Values)
                item.SetOffState(state);
        }

        public abstract void OpenPage();

        public virtual void RateUs(Action<bool> onRateDone)
        {
            UnityEngine.Application.OpenURL(VendorLink);
        }

        protected void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }

        private PendingOrder GetOrderByProductId(string productId)
        {
            if (_orderTable == null)
                return null;

            foreach (PendingOrder order in _orderTable.Values)
            {
                foreach (CartItem item in order.CartOrdered.Items())
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
