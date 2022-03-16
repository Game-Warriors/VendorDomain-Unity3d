using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;

#if UNITY_IOS || UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.iOS;

namespace GameWarriors.VendorDomian.Core
{
    public class ZarinpalIosHandler : MonoBehaviour, IMarketHandler
    {
        [DllImport("__Internal")]
        static extern void _startPurchase(string sku,string accessToken);
        [DllImport("__Internal")]
        static extern void _initialize(string payRequestUrl, string appId, string scheme, string host);

        private IVendorEventHandler _vendorEvent;
        private IPaymentServer _paymentServer;

        public string MarketId => "ZarinpaliOS";
        public string MarketPackageName => "itms-apps://";
        public string VendorLink => "https://apps.apple.com/us/app/clc-ba/id1543807261";
        public int UnconsumePurchaseCount => _unconsumePurchases?.Count ?? 0;

        public bool HasValidation => false;

        public EVendorType VendorType => EVendorType.Apple;

        private Dictionary<string, VendorPurchaseItem> _productsTable;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;

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
            _initialize(_paymentServer.RequestPayUrl, Application.identifier, "clc", "paymentresult");
#if DEVELOPMENT
            Debug.Log("Zarinpal ios Initialized");
#endif

        }

        public void Dispose()
        {
            
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

        public void RefreshPruchases(string sku)
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
                _vendorEvent.UpdatePurchaseItems(IterateOverPurchaseItem());
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
                    _vendorEvent.PurchasedSuccessful(GetProductNameById(item.ItemId), "IRR", (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, item.PurchaseToken);
            }
        }

        public async void TryBuyProduct(string sku, string payload)
        {
            _startPurchase(sku, await _paymentServer.GetAuthorizationAsync());
        }

        private async void OnPurchaseSucceed(string data)
        {
#if DEVELOPMENT
            Debug.Log("Success Purhcase:" + data);
#endif
            ZarinSuccessPurchase purhcase = JsonUtility.FromJson<ZarinSuccessPurchase>(data);
            //Debug.Log("athority : " + purhcase.Authority);
            HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, purhcase.Authority, EMarketProvider.Zarinpal);
            //Debug.Log(httpStatus);
            if (httpStatus == HttpStatusCode.OK)
                _vendorEvent.PurchasedSuccessful(GetProductNameById(purhcase.Sku),"IRR", (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, purhcase.Authority);
            else
                _vendorEvent.ConsumeFailed(purhcase.Sku, purhcase.Authority);
        }

        private void OnPurchaseCancel(string sku)
        {
            if (!string.IsNullOrEmpty(sku))
            {
#if DEVELOPMENT
                Debug.Log("OnPurchaseCancel sku: " + sku);
#endif
            }
            _vendorEvent.UserCancelPurchase("Zarrinpal purhcase cancle order : " + sku);
        }

        private void OnPurchaseFailed(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {

            }
            _vendorEvent.OnError(0, "Purchase failed : " + message);
        }

        private void PaymentRequestError(string errorMessage)
        {
            _vendorEvent.OnError(20, errorMessage);
        }

        private void OnStartPurchaseCancel(string sku)
        {
            _vendorEvent.UserCancelPurchase("Zarrinpal Start purhcase cancle Sku: " + sku);
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

        private IEnumerable<VendorPurchaseItem> IterateOverPurchaseItem()
        {
            foreach (VendorPurchaseItem item in _productsTable.Values)
            {
                yield return item;
            }
        }
    }
}
#endif