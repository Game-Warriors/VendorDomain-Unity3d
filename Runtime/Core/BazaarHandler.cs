using System;
using System.Collections.Generic;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;


namespace GameWarriors.VendorDomian.Core
{
#if BAZAAR
using BazaarPlugin;
    public class BazaarHandler : IMarketHandler
    {

        private Dictionary<string, VendorPurchaseItem> _productsTable;
        private IVendorEventHandler _vendorEventHandler;
        public string MarketId => "Bazaar";

        public string VendorLink => $"https://cafebazaar.ir/app/{Application.identifier}/?l=fa";

        public int UnconsumePurchaseCount => 0;

        public bool HasValidation => true;

        public EVendorType VendorType => EVendorType.Bazaar;

        public string MarketPackageName => "com.farsitel.bazaar";

        public void Initialization(IServiceProvider serviceProvider)
        {
            _vendorEventHandler = serviceProvider.GetService(typeof(IVendorEventHandler)) as IVendorEventHandler;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;

            VendorConfigurationObject resource = Resources.Load<VendorConfigurationObject>("BazaarVendorConfig");
            if (resource == null)
                return;
            _productsTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            resource.FillItemDic(_productsTable);

            IABEventManager.billingNotSupportedEvent += BazaarNotSupport;
            IABEventManager.purchaseSucceededEvent += PurchaseSuccess;
            IABEventManager.consumePurchaseFailedEvent += PurchaseFailed;
            IABEventManager.consumePurchaseSucceededEvent += ConsumeSuccess;
            IABEventManager.consumePurchaseFailedEvent += ConsumeFailed;
            BazaarConfigData configData = Resources.Load<BazaarConfigData>(BazaarConfigData.RESOURCES_PATH);
            BazaarIAB.init(configData.ApiKey);
        }

        public VendorPurchaseItem GetProductByName(string id)
        {
            return _productsTable[id];
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

        public void TryBuyProduct(string sku, string payload)
        {
            BazaarIAB.purchaseProduct(sku, payload);
        }

        public void Dispose()
        {

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

        public void SetProdcutSalesOffState(string itemName, bool offState)
        {
            if (_productsTable.ContainsKey(itemName))
            {
                VendorPurchaseItem item = _productsTable[itemName];
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

        public void FetchUnconsumePurchases()
        {
            return;
        }

        public void ResolveLastUnconsumePurchase()
        {
            return;
        }

        public void RefreshPruchases(string sku)
        {
            Debug.Log("RefreshPruchases");
            //BazaarBilling.GetPurchases(
            //(result) =>
            //{
            //    if (result?.Successful ?? false)
            //    {
            //        List<CafeBazaar.Billing.Purchase> purchases = result.Body;
            //        if (purchases.Count > 0)
            //        {
            //            CafeBazaar.Billing.Purchase purchase = purchases[0];
            //            _billingService.PurchasedSuccessful(GetProductById(purchase.ProductId), purchase.PurchaseTime.ToBinary(), purchase.PurchaseToken);
            //        }
            //    }
            //    else
            //    {
            //        _billingService.PurchasedFailed(5000, result.Message);
            //    }
            //});
        }

        private void PurchaseFailed(string message)
        {
            _vendorEventHandler.PurchasedFailed(0, message);
            Debug.LogError("PurchaseFailed : " + message);
        }

        private void ConsumeFailed(string message)
        {
            _vendorEventHandler.ConsumeFailed("None", message);
        }

        private void ConsumeSuccess(BazaarPurchase purchase)
        {
            _vendorEventHandler.PurchasedSuccessful(GetProductNameById(purchase.ProductId), "IRR", ToDateFromBazaar(purchase.PurchaseTime).ToBinary(), purchase.PurchaseToken);
        }

        private void PurchaseSuccess(BazaarPurchase purchase)
        {
            BazaarIAB.consumeProduct(purchase.ProductId);
        }

        private void BazaarNotSupport(string message)
        {
            Debug.LogError(message);
            _vendorEventHandler.StoreInitializeFailed();
        }

        private static DateTime ToDateFromBazaar(long miliSeconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(miliSeconds).ToLocalTime();
        }
    }
#endif
}
