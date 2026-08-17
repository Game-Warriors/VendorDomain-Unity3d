using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using UnityEngine;

#if GOOGLE
namespace GameWarriors.VendorDomian.Core
{
    using System.Collections.Generic;
    using UnityEngine.Purchasing;
    using static UnityEditor.ObjectChangeEventStream;


    public class GoogleHandler : IMarketHandler
    {
        // Apple App Store-specific product identifier for the subscription product.
        private const string kProductNameAppleSubscription = "com.unity3d.subscription.new";
        // Google Play Store-specific product identifier subscription product.
        private const string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";

        private StoreController _storeController;

        private IVendorEventHandler _vendorEventHandler;
        private Dictionary<string, VendorPurchaseItem> _productsTable;

        public string MarketId => "GooglePlay";
        public string MarketPackageName => "com.android.vending";
        public string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public int UnconsumePurchaseCount => 0;

        public EVendorType VendorType => EVendorType.Google;

        public bool HasValidation => false;

        public void Dispose()
        {
            return;
        }

        public async void Initialization(IServiceProvider serviceProvider)
        {
            IVendorEventHandler vendorEventHandler = serviceProvider.GetService(typeof(IVendorEventHandler)) as IVendorEventHandler;
            IPaymentServer paymentServer = serviceProvider.GetService(typeof(IPaymentServer)) as IPaymentServer;
            VendorConfigurationObject resource = Resources.Load<VendorConfigurationObject>("GoogleVendorConfig");
            if (resource == null)
                return;
            _productsTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            resource.FillItemDic(_productsTable);
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnStoreConnected += StoreConnected;

            try
            {
                await _storeController.Connect();
            }
            catch (System.Exception e)
            {
                Debug.LogError("IAP connection failed: " + e);
                _vendorEventHandler.OnStoreInitializeFailed();
                return;
            }


            _vendorEventHandler = vendorEventHandler;
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log("Products fetched: " + products.Count);

            _storeController.FetchPurchases();
        }

        private void OnPurchasePending(PendingOrder order)
        {
            Debug.Log("Purchase pending");

            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;

                _vendorEventHandler.PurchasedSuccessful(GetProductNameById(product.definition.id), product.metadata.isoCurrencyCode, (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, order.Info.TransactionID);
            }

            // IMPORTANT:
            // Tell the store that the purchase has been processed.
            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            Debug.Log("Purchases fetched");

            foreach (var order in orders.ConfirmedOrders)
            {
                foreach (var item in order.CartOrdered.Items())
                {
                    Product product = item.Product;

                    if (product.type == ProductType.Subscription)
                    {
                        SubscriptionInfo subscriptionInfo =
                new SubscriptionInfo(product);

                        var subscribed = subscriptionInfo.IsSubscribed();
                        var expired = subscriptionInfo.IsExpired();
                        var cancelled = subscriptionInfo.IsCancelled();
                        var autoRenewing = subscriptionInfo.IsAutoRenewing();

                        Debug.Log($"Subscribed: {subscribed}");
                        Debug.Log($"Expired: {expired}");
                        Debug.Log($"Cancelled: {cancelled}");
                        Debug.Log($"Auto renewing: {autoRenewing}");
                        Debug.Log($"Expires: {subscriptionInfo.GetExpireDate()}");
                        _vendorEventHandler.OnSubscriptionUpdate(product)
                    }
                }
            }
        }

        private void StoreConnected()
        {
            var products = new List<ProductDefinition>();
            foreach (var item in _productsTable.Values)
            {
                products.Add(new ProductDefinition(item.ProductId, (ProductType)item.Type));
            }

            _storeController.FetchProducts(products);
        }

        public void RefreshPruchases(string sku)
        {
            return;
        }

        public void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public void RateUs(Action<bool> onRateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }


        public void FetchUnconsumePurchases()
        {
            return;
        }

        public void ResolveLastUnconsumePurchase()
        {
            return;
        }

        public void TryBuyProduct(string sku, string payload)
        {
            //Debug.Log("try to buy : " + sku);
            if (_controller == null || _controller.products == null)
            {
                _vendorEventHandler.PurchasedFailed(0, "products is null , purchaseId:" + sku);
                return;
            }
            // ... look up the Product reference with the general product identifier and the Purchasing 
            // system's products collection.
            Product product = _controller.products.WithID(sku);
            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: {0}", product.definition.id));
                // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                // asynchronously.
                _controller.InitiatePurchase(product, Guid.NewGuid().ToString());
            }
            // Otherwise ...
            else
            {
                // ... report the product look-up failure situation  
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            this._controller = controller;
            this._extensions = extensions;
            foreach (var item in controller.products.all)
            {
                string sku = item.definition.id;
                var product = _productsTable[sku];
                product.SetPrice((float)item.metadata.localizedPrice);
                _productsTable[sku] = product;
            }

            _vendorEventHandler.OnPurchaseItemUpdate(IterateOverPurchaseItem());
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            //Debug.Log(p);
            if (p == PurchaseFailureReason.UserCancelled)
            {
                _vendorEventHandler.UserCancelPurchase("User Cancel");
            }
            else
            {
                _vendorEventHandler.OnError(0, $"Google Purchase Failed Item:{i.definition.id} : " + p.ToString());
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

        private IEnumerable<VendorPurchaseItem> IterateOverPurchaseItem()
        {
            foreach (VendorPurchaseItem item in _productsTable.Values)
            {
                yield return item;
            }
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