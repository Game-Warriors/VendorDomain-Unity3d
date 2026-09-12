using GameWarriors.VendorDomian.Abstraction;
using System;
using UnityEngine;

#if APPLE
namespace GameWarriors.VendorDomian.Core
{
    using GameWarriors.VendorDomian.Constants;
    using UnityEngine.iOS;
    using UnityEngine.Purchasing;

    public sealed class AppleHandler : UnityIapMarketHandlerBase , IMarketHandler
    {
        private string _vendorLink;

        public override string Id => MarketId.APPLE;
        public override string MarketPackageName => "itms-apps://";
        public override string VendorLink => _vendorLink;

        public AppleHandler(IVendorResourceLoader resourceLoader) : base(resourceLoader)
        {
        }

        protected override StoreController CreateStoreController()
        {
            return UnityIAPServices.StoreController(AppleAppStore.Name);
        }

        protected override void OnCatalogLoaded(IVendorConfigurationObject resource)
        {
            _vendorLink = "https://apps.apple.com/" + resource.StoreUrl;
        }

        protected override void RequestPurchasesRefresh()
        {
            _storeController.RestoreTransactions((success, error) =>
            {
                if (!success)
                {
                    _isFetchingPurchases = false;
                    _vendorEventListener?.OnError(Id, 0, error ?? "Apple purchase restore failed.");
                    return;
                }

                _storeController?.FetchPurchases();
            });
        }

        public void ResolveLastUnconsumePurchase()
        {
        }

        public override void OpenPage()
        {
            Application.OpenURL(VendorLink);
        }

        public override void RateUs(Action<bool> onRateDone)
        {
            onRateDone?.Invoke(Device.RequestStoreReview());
        }
    }
}
#endif
