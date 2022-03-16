
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;

#if GOOGLE
using Google.Play.Common;
using Google.Play.Review;
namespace GameWarriors.VendorDomian.Core
{
    public class ZarinpalAndroidHandler : MonoBehaviour, IMarketHandler
    {
        private AndroidJavaClass _zarinpalActivity;
        private IBillingService _billingService;
        private IPaymentServer _paymentServer;
        private Dictionary<string, VendorPurchaseItem> _productsTable;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;

        public string MarketPackageName => "com.android.vending";
        public string PriceUnit => "T";
        public bool IsInitialized => true;
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;
        public string MarketId => "ZarinpalAndroid";
        public int UnconsumePurchaseCount => _unconsumePurchases?.Count ?? 0;

        public EVendorType VendorType => EVendorType.Google;

        public bool HasValidation => false;

        public void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventHandler vendorEvent = serviceProvider.GetService(typeof(IVendorEventHandler)) as IVendorEventHandler;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            VendorConfigurationObject resource = Resources.Load<VendorConfigurationObject>("ZarinpalVendorConfig");
            if (resource == null)
                return;
            _productsTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            _unconsumePurchases = new Stack<UnconsumePurchase>(5);
            resource.FillItemDic(_productsTable);
#if DEVELOPMENT
            Debug.Log("Zarinpal Initialized");
#endif
            _zarinpalActivity = new AndroidJavaClass("com.Ario.zarinpal.ZarinpalActivity");
            _zarinpalActivity.CallStatic("initialize", paymentServer.RequestPayUrl, Application.identifier, "clc", "paymentresult");

        }

        public void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void RateUs(Action<bool> rateDone)
        {
            ReviewManager reviewManager = new ReviewManager();
            Google.Play.Common.PlayAsyncOperation<PlayReviewInfo, ReviewErrorCode> requestFlowOperation = reviewManager.RequestReviewFlow();
            requestFlowOperation.Completed += (operation) =>
            {
                PlayReviewInfo playReviewInfo = requestFlowOperation.GetResult();

                PlayAsyncOperation<VoidResult, ReviewErrorCode> launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);

                launchFlowOperation.Completed += (input) =>
                {
                    rateDone?.Invoke(launchFlowOperation.Error == ReviewErrorCode.NoError);
                };
            };
            //Application.OpenURL("market://details?id=" + Application.identifier);
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
                _billingService.UpdateMarketData();
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
                    _billingService.PurchasedSuccessful(GetProductNameById(item.ItemId), "IRR", (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, item.PurchaseToken);
                else
                    _billingService.ConsumeFailed(item.ItemId, item.PurchaseToken);
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

        public void RefreshPruchases(string sku)
        {
            Debug.Log("RefreshPruchases");
        }

        public void Dispose()
        {
            Debug.Log("Dispose");
        }

        public void SetProductId(string name, string newId)
        {
            if (_productsTable.ContainsKey(name))
            {
                var item = _productsTable[name];
                item.SetId(newId);
                _productsTable[name] = item;
            }
        }

        public VendorPurchaseItem GetProductByName(string id)
        {
            if (_productsTable.TryGetValue(id, out var item))
            {
                return item;
            }
            return default;
        }

        public VendorPurchaseItem GetProductNameById(string productId)
        {
            foreach (var item in _productsTable.Values)
            {
                if (string.Compare(item.ProductId, productId) == 0 || string.Compare(item.OffProductId, productId) == 0)
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
            ZarinSuccessPurchase purhcase = JsonUtility.FromJson<ZarinSuccessPurchase>(data);

            HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, purhcase.Authority, EMarketProvider.Zarinpal);
            if (httpStatus == HttpStatusCode.OK)
                _billingService.PurchasedSuccessful(GetProductNameById(purhcase.Sku), "IRR", (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, purhcase.Authority);
            else
                _billingService.ConsumeFailed(purhcase.Sku, purhcase.Authority);
        }

        private void OnPurchaseCancel(string sku)
        {
            if (!string.IsNullOrEmpty(sku))
            {
#if DEVELOPMENT
                Debug.Log("OnPurchaseCancel sku: " + sku);
#endif
            }
            _billingService.UserCancelPurchase("Zarrinpal purhcase cancel sku : " + sku);
        }

        private void OnPurchaseFailed(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {

            }
            _billingService.OnError(0, "Purchase failed : " + message);
        }

        private void PaymentRequestError(string errorMessage)
        {
            _billingService.OnError(20, errorMessage);
        }

        private void OnStartPurchaseCancel(string sku)
        {
            _billingService.UserCancelPurchase("Zarrinpal Start purhcase cancle Sku: " + sku);
        }

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsTable.ContainsKey(itemName))
            {
                var item = _productsTable[itemName];
                item.SetOffState(offState);
            }
        }

        public void SetAllProdcutSalesOffState(bool state)
        {
            foreach (var item in _productsTable.Values)
            {
                item.SetOffState(state);
            }
        }
    }
}
#endif