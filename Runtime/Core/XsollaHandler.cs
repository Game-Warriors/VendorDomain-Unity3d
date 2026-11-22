using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;


namespace GameWarriors.VendorDomian.Core
{
#if XSOLLA
    using Xsolla.SDK.Common;
    using Xsolla.SDK.UnityPurchasing;
    using UnityEngine.Purchasing;
    using UnityEngine.Purchasing.Extension;

    /// <summary>
    /// Drives the <see cref="XsollaPurchasingStore"/> directly, without registering it in the
    /// Unity IAP services, and listens to the store callbacks itself.
    /// </summary>
    public class XsollaHandler : IMarketHandler, IStoreConnectCallback, IStoreProductsCallback,
        IStorePurchaseCallback, IStorePurchaseFetchCallback, IStorePurchaseConfirmCallback
    {
        private string _key;
        private int _projectId;
        private bool _isTestMode;
        private bool _isFetchingProducts;
        private bool _isFetchingPurchases;
        private EStoreSetupState _state;

        private XsollaPurchasingStore _store;
        private IVendorEventListener _vendorEventListener;
        private TaskCompletionSource<bool> _connectingTask;
        private readonly HashSet<string> _confirmingTransactions = new();
        private readonly HashSet<string> _availableSkus = new();
        private readonly HashSet<long> _knownSubscriptionIds = new();
        private readonly HashSet<string> _serverCheckoutPlans = new();
        private readonly Dictionary<string, ISubscriptionInfo> _subscriptionsTable = new();
        private XsollaSubscriptionsClient _subscriptionsClient;
        private bool _hasVirtualProducts;
        private string _pendingSubscriptionPlanId;
        private bool _isPollingSubscription;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, PendingOrder> _orderTable;

        public string Id => MarketId.XSOLLA;
        public string MarketPackageName => throw new NotSupportedException();
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public bool HasValidation => false;
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
                if (_orderTable == null)
                    yield break;

                foreach (var item in _orderTable)
                {
                    yield return new PendingPurchaseData(GetProductNameById(GetProductId(item.Value)), item.Key);
                }
            }
        }

        public IEnumerable<IDelayPurchaseItem> DelayPurchaseItems => Array.Empty<IDelayPurchaseItem>();

        public XsollaHandler(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {
        }

        public void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener
                ?? throw new InvalidOperationException($"{nameof(IVendorEventListener)} is not registered.");
            if (_store != null)
                return;

            _store = CreateStore();
            _store.SetStoreConnectionCallback(this);
            _store.SetProductsCallback(this);
            _store.SetPurchaseCallback(this);
            _store.SetPurchaseFetchCallback(this);
            _store.SetPurchaseConfirmCallback(this);
            Application.focusChanged += OnApplicationFocusChanged;

            _ = TryConnecting();
        }

        public void Dispose()
        {
            if (_store == null)
                return;

            _store.SetStoreConnectionCallback(null);
            _store.SetProductsCallback(null);
            _store.SetPurchaseCallback(null);
            _store.SetPurchaseFetchCallback(null);
            _store.SetPurchaseConfirmCallback(null);
            Application.focusChanged -= OnApplicationFocusChanged;
            _store = null;
            _subscriptionsClient = null;
            _confirmingTransactions.Clear();
            _knownSubscriptionIds.Clear();
            _serverCheckoutPlans.Clear();
            _subscriptionsTable.Clear();
            _pendingSubscriptionPlanId = null;
            _isFetchingProducts = false;
            _isFetchingPurchases = false;
            _state = EStoreSetupState.None;
            CompleteConnecting(false);
        }

        private XsollaPurchasingStore CreateStore()
        {
            var settings = XsollaClientSettings.Builder.Create()
                .SetProjectId(_projectId)
                .SetLoginId(_key)
                .Build();
            var configuration = XsollaClientConfiguration.Builder.Create()
                .SetSettings(settings)
                .SetSandbox(_isTestMode)
                .Build();

            var store = new XsollaPurchasingStore(configuration);
            _subscriptionsClient = new XsollaSubscriptionsClient(_projectId, () => GetAccessTokenAsync(store), _isTestMode);
            return store;
        }

        private static Task<string> GetAccessTokenAsync(XsollaPurchasingStore store)
        {
            var completion = new TaskCompletionSource<string>();
            store.GetAccessToken(
                token => completion.TrySetResult(token),
                error => completion.TrySetException(new InvalidOperationException(error)));
            return completion.Task;
        }

        private async void OnApplicationFocusChanged(bool focused)
        {
            if (!focused || _isPollingSubscription || string.IsNullOrEmpty(_pendingSubscriptionPlanId))
                return;

            _isPollingSubscription = true;
            try
            {
                // Payment completion can become visible shortly after the browser returns.
                for (var attempt = 0; attempt < 10 && _store != null &&
                     !string.IsNullOrEmpty(_pendingSubscriptionPlanId); attempt++)
                {
                    RefreshPurchases(string.Empty);
                    var deadline = DateTime.UtcNow.AddSeconds(30);
                    while (_isFetchingPurchases && _store != null && DateTime.UtcNow < deadline)
                        await Task.Yield();
                    if (!string.IsNullOrEmpty(_pendingSubscriptionPlanId))
                        await Task.Delay(3000);
                }
            }
            finally
            {
                _isPollingSubscription = false;
            }
        }

        private Task<bool> TryConnecting()
        {
            if (_store == null)
                return Task.FromResult(false);
            if (_connectingTask != null)
                return _connectingTask.Task;

            // The store may answer synchronously, so the task is kept before connecting.
            var connectingTask = new TaskCompletionSource<bool>();
            _connectingTask = connectingTask;
            SetState(EStoreSetupState.Initializing);
            _store.Connect();
            return connectingTask.Task;
        }

        private void CompleteConnecting(bool isSuccess)
        {
            TaskCompletionSource<bool> connectingTask = _connectingTask;
            _connectingTask = null;
            connectingTask?.TrySetResult(isSuccess);
        }

        private void OnLoadDone(IVendorConfigurationObject resource)
        {
            if (resource == null)
            {
                _productsNameTable = new();
                _productsSkuTable = new();
                _vendorEventListener?.StoreInitializeFailed(Id, $"The resource for market id {Id} is null.");
                return;
            }

            _key = resource.StoreKey;
            _projectId = resource.StoreId;
            _isTestMode = resource.IsTestMode;
            _productsNameTable = new(resource.ItemCounts);
            _productsSkuTable = new(resource.ItemCounts);
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

        void IStoreConnectCallback.OnStoreConnectionSucceeded()
        {
            SetState(EStoreSetupState.Initialized);
            CompleteConnecting(true);
            _vendorEventListener?.StoreInitializeSuccess(Id);
            RefreshProducts();
        }

        void IStoreConnectCallback.OnStoreConnectionFailed(StoreConnectionFailureDescription failureDescription)
        {
            SetState(EStoreSetupState.None);
            CompleteConnecting(false);
            _vendorEventListener?.StoreInitializeFailed(Id, failureDescription.Message);
        }

        public void RefreshProducts()
        {
            if (_isFetchingProducts || _store == null || _productsNameTable == null)
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
                if (item.Type != EProductType.Subscription)
                    products.Add(new ProductDefinition(item.Id, (ProductType)item.Type));
            }
            if (products.Count == 0 && _productsNameTable.Count == 0)
                return;

            _isFetchingProducts = true;
            _hasVirtualProducts = products.Count > 0;
            if (_hasVirtualProducts)
                _store.FetchProducts(products);
            else
                ((IStoreProductsCallback)this).OnProductsFetched(Array.Empty<ProductDescription>());
        }

        public void RefreshPurchases(string sku)
        {
            if (_store == null || _isFetchingPurchases)
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

        private void BeginPurchasesRefresh()
        {
            if (_store == null || _isFetchingPurchases)
                return;

            _isFetchingPurchases = true;
            if (_hasVirtualProducts)
                _store.FetchPurchases();
            else
                RefreshSubscriptionsWithoutVirtualPurchases();
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            IProductItem purchaseItem = GetProductNameById(sku);
            if (_store == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, 0, "store not initializaed");
                return;
            }

            if (NotInitialize)
            {
                bool isSuccess = await TryConnecting();
                if (!isSuccess || _store == null)
                {
                    return;
                }
            }

            if (purchaseItem == null)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)PurchaseFailureReason.NotSupported, "product not found");
                return;
            }

            if (!_availableSkus.Contains(purchaseItem.Type == EProductType.Subscription ? purchaseItem.Id : sku))
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)PurchaseFailureReason.ProductUnavailable, "product not available");
                return;
            }

            if (purchaseItem.Type == EProductType.Subscription)
            {
                if (_serverCheckoutPlans.Contains(purchaseItem.Id))
                {
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem,
                        (int)PurchaseFailureReason.PurchasingUnavailable,
                        "Subscription plans with a first-payment setup fee require a server-generated checkout link.");
                    return;
                }
                try
                {
                    var checkoutUrl = await _subscriptionsClient.GetCheckoutUrl(purchaseItem.Id);
 ;
                    _pendingSubscriptionPlanId = purchaseItem.Id;
                    Application.OpenURL(checkoutUrl);
                }
                catch (Exception exception)
                {
                    _pendingSubscriptionPlanId = null;
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem,
                        (int)PurchaseFailureReason.PurchasingUnavailable, exception.Message);
                }
                return;
            }

            _store.InitiatePurchase(sku, null);
        }

        public bool ConsumePurchase(string transactionId)
        {
            if (_store == null || _orderTable == null ||
                !_orderTable.TryGetValue(transactionId, out PendingOrder order) ||
                !_confirmingTransactions.Add(transactionId))
                return false;

            _store.FinishTransaction(order);
            return true;
        }

        void IStorePurchaseCallback.OnPurchaseSucceeded(PendingOrder order)
        {
            ProcessPendingOrder(order, EPurchaseOrigin.FreshPurchase);
        }

        void IStorePurchaseCallback.OnPurchaseFailed(FailedOrder failedOrder)
        {
            foreach (CartItem item in failedOrder.CartOrdered.Items())
            {
                string productId = item.Product.definition.id;
                if (string.IsNullOrEmpty(productId))
                    continue;

                IProductItem purchaseItem = GetProductNameById(productId);
                if (failedOrder.FailureReason == PurchaseFailureReason.UserCancelled)
                    _vendorEventListener.UserCancelPurchase(Id, purchaseItem, (int)failedOrder.FailureReason, failedOrder.Details);
                else
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)failedOrder.FailureReason, failedOrder.Details);
            }
        }

        /// <summary>
        /// The Xsolla store never defers a purchase.
        /// </summary>
        void IStorePurchaseCallback.OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
        }

        private void ProcessPendingOrder(PendingOrder order, EPurchaseOrigin purchaseOrigin)
        {
            _orderTable ??= new();
            _orderTable.TryAdd(order.Info.TransactionID, order);

            IProductItem purchaseItem = GetProductNameById(GetProductId(order));
            _vendorEventListener.PurchasedSuccessful(Id, purchaseItem,
                purchaseItem?.ItemMeta?.CurrencyCode, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                order.Info.Receipt, order.Info.TransactionID, purchaseOrigin);
        }

        void IStorePurchaseConfirmCallback.OnConfirmOrderSucceeded(string transactionId)
        {
            _confirmingTransactions.Remove(transactionId);
            if (_orderTable == null || !_orderTable.Remove(transactionId, out PendingOrder order))
                return;

            foreach (CartItem item in order.CartOrdered.Items())
            {
                IProductItem purchaseItem = GetProductNameById(item.Product.definition.id);
                _vendorEventListener.ConsumeSuccess(Id, purchaseItem,
                    order.Info.Receipt, order.Info.TransactionID);
            }
        }

        void IStorePurchaseConfirmCallback.OnConfirmOrderFailed(FailedOrder failedOrder)
        {
            _confirmingTransactions.Remove(failedOrder.Info.TransactionID);
            foreach (CartItem item in failedOrder.CartOrdered.Items())
            {
                IProductItem purchaseItem = GetProductNameById(item.Product.definition.id);
                _vendorEventListener.ConsumeFailed(Id, purchaseItem,
                    failedOrder.Info.Receipt, failedOrder.Info.TransactionID);
            }
        }

        async void IStoreProductsCallback.OnProductsFetched(IReadOnlyList<ProductDescription> products)
        {
            _availableSkus.Clear();
            _serverCheckoutPlans.Clear();
            foreach (ProductDescription item in products)
            {
                _availableSkus.Add(item.storeSpecificId);
                if (_productsSkuTable.TryGetValue(item.storeSpecificId, out IProductItem product))
                {
                    product.SetPrice((float)item.metadata.localizedPrice);
                    product.SetMetaData(new UnityProductMeta(item.metadata));
                }
            }

            try
            {
                if (_subscriptionsClient != null && HasConfiguredSubscriptions())
                {
                    var plans = await _subscriptionsClient.GetPlans();
                    if (_store == null)
                        return;
                    foreach (var plan in plans)
                    {
                        if (string.IsNullOrEmpty(plan.plan_external_id) ||
                            !_productsSkuTable.TryGetValue(plan.plan_external_id, out var product) ||
                            product.Type != EProductType.Subscription)
                            continue;

                        var currency = plan.charge?.currency ?? string.Empty;
                        var amount = plan.charge?.amount ?? 0f;
                        var metadata = new ProductMetadata(
                            amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + currency,
                            plan.plan_name, plan.plan_description, currency, (decimal)amount);
                        product.SetPrice(amount);
                        product.SetMetaData(new UnityProductMeta(metadata));
                        _availableSkus.Add(plan.plan_external_id);
                        if (plan.charge?.setup_fee > 0f)
                            _serverCheckoutPlans.Add(plan.plan_external_id);
                    }

                    foreach (var configured in _productsNameTable.Values)
                    {
                        if (configured.Type == EProductType.Subscription && !_availableSkus.Contains(configured.Id))
                            Debug.LogWarning($"[Xsolla] Subscription plan '{configured.Id}' was not returned. Set Product ID to the plan's External ID in Publisher Account.");
                    }
                }
            }
            catch (Exception exception)
            {
                _vendorEventListener?.OnError(Id, 0, "Xsolla subscription plans: " + exception.Message);
            }

            if (_store == null)
                return;
            _isFetchingProducts = false;

            SetState(EStoreSetupState.FetchProducts);
            _vendorEventListener.OnProductItemsUpdate(Id);
            BeginPurchasesRefresh();
        }

        void IStoreProductsCallback.OnProductsFetchFailed(ProductFetchFailureDescription failureDescription)
        {
            _isFetchingProducts = false;
            _vendorEventListener?.OnError(Id, 0, failureDescription.Message);
            if (HasConfiguredSubscriptions())
            {
                _isFetchingProducts = true;
                ((IStoreProductsCallback)this).OnProductsFetched(Array.Empty<ProductDescription>());
            }
        }

        async void IStorePurchaseFetchCallback.OnAllPurchasesRetrieved(IReadOnlyList<Order> orders)
        {
            _orderTable ??= new();
            foreach (Order order in orders)
            {
                // Confirmed orders are the already owned products, only the unconsumed ones need recovering.
                if (order is PendingOrder pendingOrder &&
                    _orderTable.TryAdd(pendingOrder.Info.TransactionID, pendingOrder))
                {
                    IProductItem purchaseItem = GetProductNameById(GetProductId(pendingOrder));
                    _vendorEventListener.OnPendingPurchaseRecovered(Id, purchaseItem, pendingOrder.Info.TransactionID);
                }
            }

            await RefreshSubscriptions();
            if (_store == null)
                return;
            _isFetchingPurchases = false;
            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        async void IStorePurchaseFetchCallback.OnPurchasesRetrievalFailed(PurchasesFetchFailureDescription failureReason)
        {
            _vendorEventListener.OnError(Id, (int)failureReason.FailureReason, failureReason.Message);
            await RefreshSubscriptions();
            if (_store == null)
                return;
            _isFetchingPurchases = false;
            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        private async void RefreshSubscriptionsWithoutVirtualPurchases()
        {
            await RefreshSubscriptions();
            if (_store == null)
                return;
            _isFetchingPurchases = false;
            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener?.OnSubscriptionsUpdate(Id);
        }

        private bool HasConfiguredSubscriptions()
        {
            if (_productsNameTable == null)
                return false;
            foreach (var item in _productsNameTable.Values)
                if (item.Type == EProductType.Subscription)
                    return true;
            return false;
        }

        private async Task RefreshSubscriptions()
        {
            if (_subscriptionsClient == null || !HasConfiguredSubscriptions())
                return;

            try
            {
                var subscriptions = await _subscriptionsClient.GetSubscriptions();
                if (_store == null)
                    return;
                _subscriptionsTable.Clear();
                foreach (var subscription in subscriptions)
                {
                    if (string.IsNullOrEmpty(subscription.plan_external_id) ||
                        !_productsSkuTable.TryGetValue(subscription.plan_external_id, out var product) ||
                        product.Type != EProductType.Subscription)
                        continue;

                    if (subscription.status != "active" && subscription.status != "non_renewing")
                        continue;

                    if (!DateTimeOffset.TryParse(subscription.date_next_charge,
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                            out var nextCharge) || nextCharge <= DateTimeOffset.UtcNow)
                        continue;

                    var isNew = _knownSubscriptionIds.Add(subscription.id);
                    _subscriptionsTable[subscription.plan_external_id] = new SubscriptionData(nextCharge.UtcDateTime);
                    if (isNew && subscription.plan_external_id == _pendingSubscriptionPlanId)
                    {
                        _pendingSubscriptionPlanId = null;
                        _vendorEventListener?.PurchasedSuccessful(Id, product,
                            product.ItemMeta?.CurrencyCode,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            subscription.id.ToString(CultureInfo.InvariantCulture),
                            subscription.id.ToString(CultureInfo.InvariantCulture),
                            EPurchaseOrigin.FreshPurchase);
                    }
                }
            }
            catch (Exception exception)
            {
                _vendorEventListener?.OnError(Id, 0, "Xsolla subscriptions: " + exception.Message);
            }
        }

        public void ResolvePendingPurchase(string transactionId)
        {
            if (_orderTable != null && _orderTable.TryGetValue(transactionId, out var order))
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
            if (_productsNameTable != null && _productsNameTable.TryGetValue(itemName, out var product) &&
                _subscriptionsTable.TryGetValue(product.Id, out var subscription))
                return subscription;
            return null;
        }

        public IDelayPurchaseItem GetDelayPurchaseItemByName(string itemName)
        {
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

        public void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void RateUs(Action<bool> rateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        private void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }

        private static string GetProductId(Order order)
        {
            return order.CartOrdered.Items()[0].Product.definition.id;
        }
    }
#endif
}
