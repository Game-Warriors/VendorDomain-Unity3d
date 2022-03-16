using System;
using UnityEngine;
using GameWarriors.VendorDomian.Data;


namespace GameWarriors.VendorDomian.Core
{
#if APPLE
    using System.Collections.Generic;
    using UnityEngine.iOS;
    using UnityEngine.Purchasing;
    using GameWarriors.VendorDomian.Abstraction;

    public class AppleHandler : IMarketHandler, IStoreListener
    {
        // Apple App Store-specific product identifier for the subscription product.
        private const string kProductNameAppleSubscription = "com.unity3d.subscription.new";
        // Google Play Store-specific product identifier subscription product.
        private const string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";

        private IStoreController _controller;
        private IExtensionProvider _extensions;

        private IBillingService _billingService;
        private Dictionary<string, VendorPurchaseItem> _productsTable;

        public string MarketId => "AppStore";
        public string VendorPackageName => "itms-apps://";
        public string VendorLink => "https://apps.apple.com/us/app/clc-ba/id1543807261";

        public int UnconsumePurchaseCount => 0;

        public bool HasValidation => false;

        public void Dispose()
        {
            return;
        }

        public void Initialization(IBillingService billingService, IPaymentServer paymentServer)
        {
            VendorConfigurationObject resource = Resources.Load<VendorConfigurationObject>("AppleVendorConfig");
            if (resource == null)
                return;
            _productsTable = new Dictionary<string, VendorPurchaseItem>(resource.ItemCounts);
            resource.FillItemDic(_productsTable);
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.AppleAppStore));
            foreach (var item in _productsTable.Values)
            {
                builder.AddProduct(item.ProductId, ProductType.Consumable);
            }
            _billingService = billingService;
            UnityPurchasing.Initialize(this, builder);
        }

        public void RefreshPruchases(string sku)
        {
            return;
        }

        public void OpenPage()
        {
            Application.OpenURL("https://apps.apple.com/us/app/clc-ba/id1543807261");
        }

        public void RateUs(Action<bool> onRateDone)
        {
            bool result = Device.RequestStoreReview();
            onRateDone?.Invoke(result);
            //Application.OpenURL("https://apps.apple.com/us/app/clc-ba/id1543807261");
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
            if (_controller == null || _controller.products == null)
            {
                _billingService.PurchasedFailed(0, "products is null , purchaseId:" + sku);
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

            _billingService.OnPurchaseItemUpdate(IterateOverPurchaseItem());
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            //Debug.LogError("Google IAP Failed : " + error.ToString());
            _billingService.OnStoreInitializeFailed();
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            //Debug.Log(p);
            if (p == PurchaseFailureReason.UserCancelled)
            {
                _billingService.UserCancelPurchase("User Cancel");
            }
            else
            {
                _billingService.OnError(0, $"Apple Purchase Failed Item:{i.definition.id} : " + p.ToString());
            }
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;
            //Debug.Log(args.purchasedProduct);
            //Debug.Log(args.purchasedProduct.definition.id);
            // A consumable product has been purchased by this user.
            if (product.definition.type == ProductType.Consumable)
            {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", product.definition.id));
                string productId = product.definition.id;
                _billingService.PurchasedSuccessful(GetProductNameById(productId), args.purchasedProduct.metadata.isoCurrencyCode, (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds, product.transactionID);
            }
            // Or ... a non-consumable product has been purchased by this user.
            else if (product.definition.type == ProductType.NonConsumable)
            {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                // TODO: The non-consumable item has been successfully purchased, grant this item to the player.
            }
            // Or ... a subscription product has been purchased by this user.
            else if (product.definition.type == ProductType.Subscription)
            {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                // TODO: The subscription item has been successfully purchased, grant this to the player.
            }
            // Or ... an unknown product has been purchased by this user. Fill in additional products here....
            else
            {
                _billingService.OnError(0, $"Apple Purchase Complete Error Item:{args.purchasedProduct.definition.id}");
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }

            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed. 
            Debug.Log("PurchaseProcessingResult.Complete");
            return PurchaseProcessingResult.Complete;
        }

        public void SetProductId(string name, string newId)
        {
            if (_productsTable.TryGetValue(name, out var item))
            {
                item.SetId(newId);
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
#endif
}
