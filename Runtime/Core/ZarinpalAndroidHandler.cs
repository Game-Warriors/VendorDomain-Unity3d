
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

#if GOOGLE
namespace GameWarriors.VendorDomian.Core
{
    public class ZarinpalAndroidHandler : MonoBehaviour, IMarketHandler
    {
        private AndroidJavaClass _zarinpalActivity;
        private IVendorEventListener _eventListener;
        private IPaymentServer _paymentServer;
        private Dictionary<string, IProductItem> _productsNameTable;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;
        private EStoreSetupState _state;
        public string MarketPackageName => "com.android.vending";
        public string PriceUnit => "T";
        bool IMarketHandler.IsInitialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;
        public string Id => MarketId.ZARINPAL;
        public int? UnconsumePurchaseCount => _unconsumePurchases?.Count;
        public bool HasValidation => false;
        public bool IsLoading => _productsNameTable == null;
        public IEnumerable<IProductItem> PurchaseItems => _productsNameTable.Values;

        public IEnumerable<IPendingPurchaseItem> PendingPurchaseItems => new IPendingPurchaseItem[0];

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

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
        }

        public void Initialization(IServiceProvider serviceProvider)
        {
            _eventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            _paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;

            _unconsumePurchases = new Stack<UnconsumePurchase>(5);
#if DEVELOPMENT
            Debug.Log("Zarinpal Initialized");
#endif
            _zarinpalActivity = new AndroidJavaClass("com.Ario.zarinpal.ZarinpalActivity");
            _zarinpalActivity.CallStatic("initialize", _paymentServer.RequestPayUrl, Application.identifier, "clc", "paymentresult");
            _state = EStoreSetupState.Initialized;
        }

        public void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void RateUs(Action<bool> rateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void OnStoreInitialized(string data)
        {
            Debug.Log("store initialize by uri: " + data);
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            _zarinpalActivity.CallStatic("startPurchase", sku, await _paymentServer.GetAuthorizationAsync());
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
                IProductItem product = GetProductNameById(item.ItemId);
                HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, item.PurchaseToken, EMarketProvider.Zarinpal);
                if (httpStatus == HttpStatusCode.OK)
                    _eventListener.PurchasedSuccessful(Id, product, "IRR",
                        (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                        item.PurchaseToken, default, EPurchaseOrigin.RecoveredUnconfirmedPurchase);
                else
                    _eventListener.ConsumeFailed(Id, product, item.ItemId, item.PurchaseToken);
            }
        }

        public async void TryGetSkuDetails(IList<string> consumeProducts, IList<string> subscriptionProduct)
        {
            ////Debug.Log("TryGetSkuDetails");
            //string appVersion = Application.version;
            //(string, HttpStatusCode) result = await Task.Factory.StartNew(()
            //    => PaymentServer.GetUnconsumePurchases(GAME_ID, appVersion, _playerProfile.PlayerId, _playerProfile.SessionToken));
            ////Debug.Log("TryGetSkuDetails status : " + result.Item2);
            //try
            //{
            //    if (result.Item2 == HttpStatusCode.OK)
            //    {
            //        //Debug.Log(result.Item1);
            //        UnconsumePayments payments = await Task.Factory.StartNew(() => JsonUtility.FromJson<UnconsumePayments>(result.Item1));
            //        int length = payments.Length;
            //        for (int i = 0; i < length; ++i)
            //        {
            //            _ibillingService.OnPurchasesUpdated(new Purchase(payments[i].ItemId, 0, 0, payments[i].Token, payments[i].Token));
            //        }
            //    }
            //}
            //catch (Exception E)
            //{
            //    Debug.LogError(E.ToString());
            //}
        }

        public void RefreshPurchases(string sku)
        {
            Debug.Log("RefreshPruchases");
        }

        public void Dispose()
        {
            Debug.Log("Dispose");
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

        private void PassData(string data)
        {
            Debug.Log(data);
        }

        private async void OnPurchaseSucceed(string data)
        {
#if DEVELOPMENT
            Debug.Log("Success Purhcase:" + data);
#endif
            ZarinSuccessPurchase purchase = JsonUtility.FromJson<ZarinSuccessPurchase>(data);
            IProductItem product = GetProductNameById(purchase.Sku);
            HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, purchase.Authority, EMarketProvider.Zarinpal);
            if (httpStatus == HttpStatusCode.OK)
                _eventListener.PurchasedSuccessful(Id, product, "IRR",
                    (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                    purchase.Authority, default, EPurchaseOrigin.FreshPurchase);
            else
                _eventListener.ConsumeFailed(Id, product, purchase.Sku, purchase.Authority);
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
            _eventListener.UserCancelPurchase(Id, product, "Zarrinpal purhcase cancel sku : " + sku);
        }

        private void OnPurchaseFailed(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {

            }
            _eventListener.OnError(Id, 0, "Purchase failed : " + message);
        }

        private void PaymentRequestError(string errorMessage)
        {
            _eventListener.OnError(Id, 20, errorMessage);
        }

        private void OnStartPurchaseCancel(string sku)
        {
            IProductItem product = GetProductNameById(sku);
            _eventListener.UserCancelPurchase(Id, product, "Zarrinpal Start purhcase cancel Sku: " + sku);
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

        public void RefreshProducts()
        {
            
        }

        public ISubscriptionInfo GetSubscriptionInfoByName(string productId)
        {
            throw new NotSupportedException();
        }

        public bool ConsumePurchase(string transactionId)
        {
            return true;
        }
    }
}
#endif
