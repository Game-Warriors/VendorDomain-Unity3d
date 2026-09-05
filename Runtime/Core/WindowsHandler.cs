using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;


namespace GameWarriors.VendorDomian.Core
{
    public class WindowsHandler : IMarketHandler
    {
        private IVendorEventListener _vendorEvent;
        private IPaymentServer _paymentServer;
        private Stack<UnconsumePurchase> _unconsumePurchases;
        private bool _isFetchingUnconsume;
        private EStoreSetupState _state;
        private Dictionary<string, IProductItem> _productsNameTable;

        public string Id => MarketId.WINDOWS;
        public string MarketPackageName => string.Empty;
        public string VendorLink => string.Empty;
        public int? UnconsumePurchaseCount => _unconsumePurchases?.Count;
        public bool HasValidation => false;
        bool IMarketHandler.NotInitialize => _state == EStoreSetupState.None;
        bool IMarketHandler.Initialized => _state > EStoreSetupState.Initializing;
        bool IMarketHandler.IsProductFetched => _state > EStoreSetupState.Initialized;
        bool IMarketHandler.IsPurchasesFetched => _state > EStoreSetupState.FetchProducts;
        public bool IsLoading => _productsNameTable == null;
        public IEnumerable<IProductItem> PurchaseItems => _productsNameTable.Values;

        public IEnumerable<IPendingPurchaseItem> PendingPurchaseItems => new IPendingPurchaseItem[0];

        public WindowsHandler(IPaymentServer paymentServer)
        {
            _paymentServer = paymentServer;
        }

        public void StartLoading(IVendorResourceLoader resourceLoader)
        {
            resourceLoader.LoadAsync(Id, OnLoadDone);
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
            _state = EStoreSetupState.FetchPurchases;
        }

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

            }
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

        public IProductItem GetProductByName(string id)
        {
            return _productsNameTable[id];
        }

        public void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventListener vendorEventListener = serviceProvider.GetService(typeof(IVendorEventListener)) as IVendorEventListener;
            _vendorEvent = vendorEventListener;
            _unconsumePurchases = new Stack<UnconsumePurchase>(5);
        }

        public void OpenPage()
        {

        }

        public void RateUs(Action<bool> rateDone)
        {

        }

        public void RefreshPurchases(string sku)
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
                IProductItem product = GetProductNameById(item.ItemId);
                HttpStatusCode httpStatus = await _paymentServer.TryToConsumePayment(Application.identifier, item.PurchaseToken, EMarketProvider.Zarinpal);
                if (httpStatus == HttpStatusCode.OK)
                    _vendorEvent.PurchasedSuccessful(Id, product, "IRR",
                        (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                        item.PurchaseToken, default, EPurchaseOrigin.RecoveredUnconfirmedPurchase);
                else
                    _vendorEvent.ConsumeFailed(Id, product, item.PurchaseToken, default);
            }
            else
                _vendorEvent.ConsumeFailed(Id, default, string.Empty, string.Empty);
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
            _vendorEvent.PurchasedSuccessful(Id, GetProductNameById(sku), "IIR", DateTime.UtcNow.ToBinary(),
                UnityEngine.Random.Range(1000000, 9000000).ToString(), string.Empty,
                EPurchaseOrigin.FreshPurchase);
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
            return default;
        }

        public bool ConsumePurchase(string transactionId)
        {
            return true;
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
