using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;


namespace GameWarriors.VendorDomian.Core
{

#if BAZAAR
    using Bazaar.Poolakey;
    using Bazaar.Poolakey.Data;
    using System.Threading.Tasks;
    using Bazaar.Data;

    public class BazaarHandler : IMarketHandler
    {
        private string _key;
        private bool _isFetchingProducts;
        private EStoreSetupState _state;
        private Payment _payment;
        private IVendorEventListener _vendorEventListener;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Dictionary<string, IProductItem> _productsSkuTable;
        private Dictionary<string, SKUDetails> _subscriptionsTable;
        private Dictionary<string, PurchaseInfo> _orderTable;
        public string Id => MarketId.BAZAAR;

        public string VendorLink => $"https://cafebazaar.ir/app/{Application.identifier}";

        public int? UnconsumePurchaseCount => 0;

        public bool HasValidation => true;


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

        public void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            SecurityCheck securityCheck = SecurityCheck.Enable(_key);
            PaymentConfiguration paymentConfiguration = new(securityCheck);
            _payment = new Payment(paymentConfiguration);
            _ = TryConnecting();
        }

        private async Task<bool> TryConnecting()
        {
            try
            {
                SetState(EStoreSetupState.Initializing);
                await _payment.Connect();
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
            _key = resource.StoreKey;
            _productsNameTable = new(resource.ItemCounts);
            _productsSkuTable = new(resource.ItemCounts);

            foreach (IProductItem product in resource.Products)
            {
                _productsNameTable.Add(product.Name, product);
                _productsSkuTable.Add(product.Id, product);
            }
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
            if (_payment == null)
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

            Result<PurchaseInfo> result = await _payment.Purchase(sku, payload: payload);
            if (result.status == Status.Success)
            {
                _vendorEventListener.PurchasedSuccessful(Id, purchaseItem, "IRR",
                                result.data.purchaseTime,
                                result.data.orderId, result.data.purchaseToken, EPurchaseOrigin.FreshPurchase);
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
            _payment.Disconnect();
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

        public async void FetchUnconsumePurchases()
        {
            var result = await _payment.GetPurchases();
        }


        public async void RefreshPurchases(string sku)
        {
            Result<List<PurchaseInfo>> result = await _payment.GetPurchases();
            if (result.status != Status.Success)
                return;
            _orderTable ??= new Dictionary<string, PurchaseInfo>();
            _orderTable.Clear();
            _subscriptionsTable.Clear();
            foreach (var item in result.data)
            {
                if (_productsSkuTable.TryGetValue(item.productId, out var prodcut))
                {
                    if (item.purchaseState == PurchaseInfo.State.Purchased && prodcut.Type == EProductType.Consumable)
                    {
                        _orderTable.Add(item.productId, item);
                    }
                    else if (item.purchaseState == PurchaseInfo.State.Purchased || item.purchaseState == PurchaseInfo.State.Consumed
                        && prodcut.Type == EProductType.Subscription )
                    {
                        SKUDetails details = prodcut as SKUDetails;
                        if (details != null && details.subscriptionExpireDate > DateTime.UtcNow)
                            _subscriptionsTable.Add(item.productId, details);
                    }
                }
            }

            SetState(EStoreSetupState.FetchPurchases);
            _vendorEventListener.OnSubscriptionsUpdate(Id);
        }


        private void BazaarNotSupport(string message)
        {
            Debug.LogError(message);
            _vendorEventListener.StoreInitializeFailed();
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
            _ = _payment.Consume(transactionId, (info) =>
            {
            });

        }

        public async void RefreshProducts()
        {
            if (_isFetchingProducts)
                return;
            var products = new List<string>();
            foreach (var item in _productsNameTable.Values)
            {
                products.Add(item.Id);
            }
            _isFetchingProducts = true;
            Result<List<SKUDetails>> result = await _payment.GetSkuDetails(products);
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

        public ISubscriptionInfo GetSubscriptionInfoByName(string itemName)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
