using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;


#if UNITY_IOS || UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.iOS;

namespace GameWarriors.VendorDomian.Core
{
    public class ZarinpalIosHandler : MonoBehaviour, IMarketHandler
    {
        [DllImport("__Internal")]
        static extern void _startPurchase(string sku, string accessToken);
        [DllImport("__Internal")]
        static extern void _initialize(string payRequestUrl, string appId, string scheme, string host);

        private IVendorEventListener _vendorEvent;
        private IPaymentServer _paymentServer;
        private EStoreSetupState _state;
        public string Id => "ZarinpaliOS";
        public string MarketPackageName => "itms-apps://";
        public string VendorLink => "https://apps.apple.com/us/app/clc-ba/id1543807261";
        public int? UnconsumePurchaseCount => _unconsumePurchases?.Count;

        public bool HasValidation => false;

        bool IMarketHandler.IsInitialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;
        public bool IsLoading => _productsNameTable == null;

        private Dictionary<string, IProductItem> _productsNameTable;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;
        public IEnumerable<IProductItem> PurchaseItems => _productsNameTable.Values;

        public bool IsProductFetched => throw new NotImplementedException();

        public bool IsPurchasesFetched => throw new NotImplementedException();

        IEnumerable<IProductItem> IMarketHandler.PurchaseItems => PurchaseItems;

        public IEnumerable<IPendingPurchaseItem> PendingPurchaseItems => throw new NotSupportedException();

        public void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventListener vendorEvent = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            _unconsumePurchases = new Stack<UnconsumePurchase>(5);
            _initialize(_paymentServer.RequestPayUrl, Application.identifier, "clc", "paymentresult");
            _state = EStoreSetupState.Initialized;
#if DEVELOPMENT
            Debug.Log("Zarinpal ios Initialized");
#endif
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void Dispose()
        {

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

        public void RefreshPurchases(string sku)
        {
            return;
        }

        public void OpenPage()
        {
            Application.OpenURL("https://apps.apple.com/us/app/clc-ba/id1543807261");
        }

        public void RateUs(Action<bool> rateDone)
        {

            bool result = Device.RequestStoreReview();
            rateDone?.Invoke(result);
            //Application.OpenURL("https://apps.apple.com/us/app/clc-ba/id1543807261");
        }

        public async void FetchUnconsumePurchases()
        {
            if (_unconsumePurchases.Count == 0 && !_isFetchingUnconsume)
            {
                _isFetchingUnconsume = true;
                IList<UnconsumePurchase> items = await _paymentServer.TryGetUnconsumePurchase(Application.identifier, EMarketProvider.Zarinpal);
                _isFetchingUnconsume = false;
                _unconsumePurchases.Clear();
                int length = items?.Count ?? 0;
                for (int i = 0; i < length; ++i)
                {
                    _unconsumePurchases.Push(items[i]);
                }
            }
        }

        public async void ResolveLastUnconsumePurchase()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return;
            }
            if (_unconsumePurchases.Count > 0)
            {
                UnconsumePurchase item = _unconsumePurchases.Pop();
                HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, item.PurchaseToken, EMarketProvider.Zarinpal);
                if (httpStatus == HttpStatusCode.OK)
                    _vendorEvent.PurchasedSuccessful(Id, GetProductNameById(item.ItemId), "IRR",
                        (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                        item.PurchaseToken, default, EPurchaseOrigin.RecoveredUnconfirmedPurchase);
            }
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            _startPurchase(sku, await _paymentServer.GetAuthorizationAsync());
        }

        private async void OnLoadDone(IVendorConfigurationObject resource)
        {
            if (resource == null)
            {
                _productsNameTable = new Dictionary<string, IProductItem>();
                throw new ArgumentNullException($"the resource for market id {Id} in null");
            }
            _productsNameTable = new Dictionary<string, IProductItem>(resource.ItemCounts);

            int length = resource.ItemCounts;
            foreach (IProductItem product in resource.Products)
            {
                _productsNameTable.Add(product.Name, product);
            }
        }

        private async void OnPurchaseSucceed(string data)
        {
#if DEVELOPMENT
            Debug.Log("Success Purhcase:" + data);
#endif
            ZarinSuccessPurchase purhcase = JsonUtility.FromJson<ZarinSuccessPurchase>(data);
            IProductItem product = GetProductNameById(purhcase.Sku);
            HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, purhcase.Authority, EMarketProvider.Zarinpal);
            //Debug.Log(httpStatus);
            if (httpStatus == HttpStatusCode.OK)
                _vendorEvent.PurchasedSuccessful(Id, product, "IRR",
                    (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                    purhcase.Authority, default, EPurchaseOrigin.FreshPurchase);
            else
                _vendorEvent.ConsumeFailed(Id, product, purhcase.Authority, default);
        }

        private void OnPurchaseCancel(string sku)
        {
            if (!string.IsNullOrEmpty(sku))
            {
#if DEVELOPMENT
                Debug.Log("OnPurchaseCancel sku: " + sku);
#endif
            }
            IProductItem product = GetProductNameById(sku);
            _vendorEvent.UserCancelPurchase(Id, product, "Zarrinpal purhcase cancle order : " + sku);
        }

        private void OnPurchaseFailed(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {

            }
            _vendorEvent.OnError(Id, 0, "Purchase failed : " + message);
        }

        private void PaymentRequestError(string errorMessage)
        {
            _vendorEvent.OnError(Id, 20, errorMessage);
        }

        private void OnStartPurchaseCancel(string sku)
        {
            IProductItem product = GetProductNameById(sku);
            _vendorEvent.UserCancelPurchase(Id, product, "Zarrinpal Start purhcase cancle Sku: " + sku);
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

        private IEnumerable<VendorPurchaseItem> IterateOverPurchaseItem()
        {
            foreach (VendorPurchaseItem item in _productsNameTable.Values)
            {
                yield return item;
            }
        }

        public void RefreshProducts()
        {

        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string productId)
        {
            throw new NotSupportedException();
        }

        public void ConsumePurchase(string transactionId)
        {
            throw new NotSupportedException();
        }
    }
}
#endif
