using GameWarriors.VendorDomian.Abstraction;
using System;
using UnityEngine;

#if GOOGLE
namespace GameWarriors.VendorDomian.Core
{
    using GameWarriors.VendorDomian.Constants;
    using UnityEngine.Purchasing;

    public class GoogleHandler : UnityIapMarketHandlerBase, IMarketHandler
    {
        public override string Id => MarketId.GOOGLE;
        public override string MarketPackageName => "com.android.vending";
        public override string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public GoogleHandler(IVendorResourceLoader resourceLoader) : base(resourceLoader)
        {
        }

        protected override StoreController CreateStoreController()
        {
            return UnityIAPServices.StoreController();
        }

        public override void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public override void RateUs(Action<bool> onRateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }
    }
}
#endif
