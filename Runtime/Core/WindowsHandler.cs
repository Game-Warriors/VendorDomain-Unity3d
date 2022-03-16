using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;


namespace GameWarriors.VendorDomian.Core
{
    public class WindowsHandler : IMarketHandler
    {
        private IVendorEventHandler _vendroEvent;
        private IPaymentServer _paymentServer;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;
        private Dictionary<string, VendorPurchaseItem> _productsTable;

        public string MarketId => "Windows";
        public string MarketPackageName => "";

        public string VendorLink => "";

        public int UnconsumePurchaseCount => _unconsumePurchases?.Count ?? 0;

        public bool HasValidation => false;

        public EVendorType VendorType => EVendorType.Windows;

        public void Dispose()
        {

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
                if (length > 0)
                    _vendroEvent.UpdatePurchaseItems(IterateOverPurchaseItem());
            }
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

        public VendorPurchaseItem GetProductByName(string id)
        {
            return _productsTable[id];
        }

        public void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventHandler vendorEvent = serviceProvider.GetService(typeof(IVendorEventHandler)) as IVendorEventHandler;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            _vendroEvent = vendorEvent;
            _paymentServer = paymentServer;
            VendorConfigurationObject resource = Resources.Load<VendorConfigurationObject>("ZarinpalVendorConfig");
            if (resource == null)
                return;
            _unconsumePurchases = new Stack<UnconsumePurchase>(5);
            _productsTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            resource.FillItemDic(_productsTable);
        }


        public void OpenPage()
        {

        }

        public void RateUs(Action<bool> rateDone)
        {

        }

        public void RefreshPruchases(string sku)
        {

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
                    _vendroEvent.PurchasedSuccessful(GetProductNameById(item.ItemId), "IRR", (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, item.PurchaseToken);
                else
                    _vendroEvent.ConsumeFailed(item.ItemId, item.PurchaseToken);
            }
            else
                _vendroEvent.ConsumeFailed(string.Empty, string.Empty);
        }

        public void SetProductId(string name, string newId)
        {

        }

        public void TryBuyProduct(string sku, string payload)
        {
            //IBackend<string> backend = ServiceLocator.Resolve<IBackend<string>>(); 
            //requestpurchase
            //backend.SendDataAsync("",new System.Threading.CancellationToken(),new RequestPurhcaseBindingModel(,sku)
            //Application.OpenURL();
            //_billingService.UserCancelPurchase(payload);
            _vendroEvent.PurchasedSuccessful(GetProductNameById(sku), "IIR", DateTime.UtcNow.ToBinary(), UnityEngine.Random.Range(1000000, 9000000).ToString());
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
public enum EPaymentProviderType : short { None, Zarinpal }

[Serializable]
public struct RequestPurhcaseBindingModel
{
    [SerializeField]
    private string ApplicationId;
    [SerializeField]
    private string ItemName;
    [SerializeField]
    private EPaymentProviderType ProviderType;

    public RequestPurhcaseBindingModel(string appId, string itemName, EPaymentProviderType providerType)
    {
        ApplicationId = appId;
        ItemName = itemName;
        ProviderType = providerType;
    }
}
