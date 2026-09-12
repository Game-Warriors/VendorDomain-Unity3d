using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using System;
using UnityEngine;


namespace GameWarriors.VendorDomian.Core
{
#if XSOLLA
    using Xsolla.SDK.Common;
    using Xsolla.SDK.UnityPurchasing;
    using UnityEngine.Purchasing;

    public class XsollaHandler : UnityIapMarketHandlerBase , IMarketHandler
    {
        private string _key;
        private int _projectId;
        private bool _isTestMode;

        public override string Id => MarketId.XSOLLA;
        public override string MarketPackageName => throw new NotSupportedException();
        public override string VendorLink => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public XsollaHandler(IVendorResourceLoader resourceLoader) : base(resourceLoader)
        {
        }

        protected override void OnCatalogLoaded(IVendorConfigurationObject resource)
        {
            _key = resource.StoreKey;
            _projectId = resource.StoreId;
            _isTestMode = resource.IsTestMode;
        }

        protected override StoreController CreateStoreController()
        {
            var settings = XsollaClientSettings.Builder.Create()
                .SetProjectId(_projectId)
                .SetLoginId(_key)
                .Build();
            var configuration = XsollaClientConfiguration.Builder.Create()
                .SetSettings(settings)
                .SetSandbox(_isTestMode)
                .Build();

            var module = XsollaPurchasingModule.Builder.Create()
                .SetConfiguration(configuration)
                .Build();

            return module.CreateStoreController();
        }

        public override void OpenPage()
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }

        public override void RateUs(Action<bool> rateDone)
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }
    }
#endif
}
