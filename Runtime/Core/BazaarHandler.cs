using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameWarriors.VendorDomian.Core
{

#if BAZAAR
    using Bazaar.Poolakey;
    using Bazaar.Poolakey.Data;
    using System.Threading.Tasks;
    using Bazaar.Data;
    using GameWarriors.VendorDomian.Data.Bazaar;

    public class BazaarHandler : IMarketHandler
    {
        private string _key;
        private bool _isFetchingProducts;
        private bool _isFetchingPurchases;
        private EStoreSetupState _state;
        private Payment _storeController;
        private IVendorEventListener _vendorEventListener;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, SKUDetails> _subscriptionsTable;
        private Dictionary<string, PurchaseInfo> _orderTable;
        public string Id => MarketId.BAZAAR;

        public string VendorLink => $"https://cafebazaar.ir/app/{Application.identifier}";

        public int? UnconsumePurchaseCount => _orderTable?.Count;

        public bool HasValidation => false;

        public string MarketPackageName => "com.farsitel.bazaar";

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
                    string id = item.Value.productId;
                    yield return new PendingPurchaseData(_productsSkuTable[id], item.Key);
                }
            }
        }

        public BazaarHandler(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {

        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            SecurityCheck securityCheck = SecurityCheck.Enable(_key);
            PaymentConfiguration paymentConfiguration = new(securityCheck);
            _storeController = new Payment(paymentConfiguration);
            await TryConnecting();
        }

        private async Task<bool> TryConnecting()
        {
            if (_storeController == null)
                return false;

            try
            {
                SetState(EStoreSetupState.Initializing);
                Result<bool> result = await _storeController.Connect();
                if (result.status != Status.Success)
                {
                    SetState(EStoreSetupState.None);
                    _vendorEventListener?.StoreInitializeFailed(Id, result.ToString());
                    return false;
                }
                SetState(EStoreSetupState.Initialized);

                if (_productsNameTable != null)
                    RefreshProducts();

                return true;
            }
            catch (Exception e)
            {
                SetState(EStoreSetupState.None);
                _vendorEventListener?.StoreInitializeFailed(Id, e.ToString());
                return false;
            }
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

            if (IsInitialized)
                RefreshProducts();
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

        public void OpenPage()
        {
            Application.OpenURL($"https://www.cafebazaar.ir/app/{Application.identifier}");
        }

        public void RateUs(Action<bool> onRateDone)
        {
            try
            {
                AndroidJavaClass uriStaticClass = new AndroidJavaClass("android.net.Uri");
                AndroidJavaClass intentStaticClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intentObjectClass = new AndroidJavaObject("android.content.Intent");
                intentObjectClass.Call<AndroidJavaObject>("setAction", intentStaticClass.GetStatic<string>("ACTION_EDIT"));
                intentObjectClass.Call<AndroidJavaObject>("setData", uriStaticClass.CallStatic<AndroidJavaObject>("parse", "bazaar://details?id=" + Application.identifier));
                intentObjectClass.Call<AndroidJavaObject>("setPackage", MarketPackageName);
                AndroidJavaClass unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityActivity.GetStatic<AndroidJavaObject>("currentActivity");
                currentActivity.Call("startActivity", intentObjectClass);
                onRateDone?.Invoke(true);
            }
            catch
            {
                onRateDone?.Invoke(false);
                Application.OpenURL($"https://www.cafebazaar.ir/app/{Application.identifier}");
            }
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

            Result<PurchaseInfo> result = await _storeController.Purchase(sku, payload: payload);
            if (result.status == Status.Success)
            {
                if (result.data.purchaseState == PurchaseInfo.State.Purchased)
                {
                    _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, "IRR",
                                    result.data.purchaseTime,
                                    result.data.orderId, result.data.purchaseToken, EPurchaseOrigin.FreshPurchase);
                }
                else
                {
                    _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)result.data.purchaseState, result.message);
                }
            }
            else if (result.status == Status.InstallBazaar)
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)result.status, result.message);
            }
            else
            {
                _vendorEventListener.PurchasedFailed(Id, purchaseItem, (int)result.status, result.message);
            }
        }

        public void Dispose()
        {
            _storeController.Disconnect();
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

        public void FetchUnconsumePurchases()
        {
            RefreshPurchases(string.Empty);
        }

        public async void RefreshPurchases(string sku)
        {
            if (_isFetchingPurchases)
                return;
            _isFetchingPurchases = true;
            Result<List<PurchaseInfo>> result = await _storeController.GetPurchases();
            _isFetchingPurchases = false;
            if (result.status != Status.Success)
                return;
            _orderTable ??= new Dictionary<string, PurchaseInfo>();
            _orderTable.Clear();
            _subscriptionsTable.Clear();
            foreach (var item in result.data)
            {
                if (_productsSkuTable.TryGetValue(item.productId, out IProductItem product))
                {
                    if (item.purchaseState == PurchaseInfo.State.Purchased && product.Type == EProductType.Consumable)
                    {
                        _orderTable.Add(item.purchaseToken, item);
                    }
                    else if (item.purchaseState == PurchaseInfo.State.Purchased || item.purchaseState == PurchaseInfo.State.Consumed
                        && product.Type == EProductType.Subscription)
                    {

                        SKUDetails details = ((BazaarProductMeta)product.ItemMeta).SKUDetail;
                        if (details != null && details.subscriptionExpireDate > DateTime.UtcNow)
                            _subscriptionsTable.Add(item.productId, details);
                    }
                }
            }

            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }

        private static DateTime ToDateFromBazaar(long miliSeconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(miliSeconds).ToLocalTime();
        }

        private void SetState(EStoreSetupState state)
        {
            _state = state;
            _vendorEventListener?.OnVendorStateChanged(Id, state);
        }

        public bool ConsumePurchase(string transactionId)
        {
            if (_storeController == null || _orderTable == null)
                return false;
            if (_orderTable.Remove(transactionId))
            {
                _ = _storeController.Consume(transactionId, result =>
                {
                    //if (result.status == Status.Success)
                    //    _orderTable.Remove(transactionId);
                });
                return true;
            }
            return false;
        }

        public async void RefreshProducts()
        {
            if (_isFetchingProducts || _storeController == null ||
                _productsNameTable == null || _state < EStoreSetupState.Initialized)
                return;
            var products = new List<string>();
            foreach (var item in _productsNameTable.Values)
            {
                products.Add(item.Id);
            }
            _isFetchingProducts = true;
            Result<List<SKUDetails>> result = await _storeController.GetSkuDetails(products);
            _isFetchingProducts = false;
            if (result.status != Status.Success)
                return;
            foreach (var item in result.data)
            {
                string sku = item.sku;
                if (_productsSkuTable.TryGetValue(sku, out IProductItem product))
                {
                    if (float.TryParse(item.price, out var floatPrice))
                        product.SetPrice(floatPrice);
                    product.SetMetaData(new BazaarProductMeta(item, floatPrice));
                }
            }
            SetState(EStoreSetupState.FetchProducts);
            _vendorEventListener.OnPurchaseItemsUpdate(Id);
            RefreshPurchases(string.Empty);
        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string productName)
        {
            if (_productsNameTable.TryGetValue(productName, out var item))
            {
                if (_subscriptionsTable.TryGetValue(item.Id, out SKUDetails info))
                    return new SubscriptionData(info.subscriptionExpireDate);
            }
            return null;
        }

    }
#endif
}
