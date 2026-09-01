using UnityEngine;
using UnityEditor;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Constants;
using System.IO;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public class VendorConfigurationMenu : ScriptableWizard
    {
        private const int SPACE = 10;
        public const string ASSET_DIRECTORY = "Assets/AssetData/Vendor";
        public readonly string BAZAAR_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.BAZAAR}VendorConfig.asset";
        public readonly string MYKET_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.MYKET}VendorConfig.asset";
        public readonly string GOOGLE_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.GOOGLE}VendorConfig.asset";
        public readonly string APPLE_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.APPLE}VendorConfig.asset";
        public readonly string ZARINPAL_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.ZARINPAL}VendorConfig.asset";
        public readonly string XSOLLA_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.XSOLLA}VendorConfig.asset";

        [Space(SPACE)]
        [SerializeField]
        private string _bazaarMarketPackUrl;
        [SerializeField]
        private string _bazaarStoreKey;
        [SerializeField]
        private VendorPurchaseItem[] _bazaarItems;

        [Space(SPACE)]
        [SerializeField]
        private string _myketMarketPackUrl;
        [SerializeField]
        private string _myketStoreKey;
        [SerializeField]
        private VendorPurchaseItem[] _myketItems;

        [Space(SPACE)]
        [SerializeField]
        private string _googleMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _googleItems;

        [Space(SPACE)]
        [SerializeField]
        private string _appleMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _appleItems;

        [Space(SPACE)]
        [SerializeField]
        private string _zarinpalMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _zarinpalItems;

        [Space(SPACE)]
        [SerializeField]
        private string _xsollaMarketPackUrl;
        [SerializeField]
        private VendorSetupConfiguration _xsollaSetupConfiguration;
        [SerializeField]
        private VendorPurchaseItem[] _xsollaItems;

        [MenuItem("Tools/Vendor Configuration")]
        private static void OpenBuildConfigWindow()
        {
            VendorConfigurationMenu tmp = DisplayWizard<VendorConfigurationMenu>("Vendor Configuration", "Save");
            tmp.Initialization();
        }

        private void Initialization()
        {
            Directory.CreateDirectory(ASSET_DIRECTORY);
            VendorConfigurationObject googleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(GOOGLE_ASSET_PATH);
            VendorConfigurationObject bazaarAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(BAZAAR_ASSET_PATH);
            VendorConfigurationObject myketAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(MYKET_ASSET_PATH);
            VendorConfigurationObject appleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(APPLE_ASSET_PATH);
            VendorConfigurationObject zarinpalAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(ZARINPAL_ASSET_PATH);
            VendorConfigurationObject xsollaAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(XSOLLA_ASSET_PATH);
            if (googleAsset != null)
            {
                _googleItems = googleAsset.Products;
                _googleMarketPackUrl = googleAsset.StoreUrl;
            }

            if (bazaarAsset != null)
            {
                _bazaarItems = bazaarAsset.Products;
                _bazaarMarketPackUrl = bazaarAsset.StoreUrl;
            }

            if (myketAsset != null)
            {
                _myketItems = myketAsset.Products;
                _myketMarketPackUrl = myketAsset.StoreUrl;
            }

            if (appleAsset != null)
            {
                _appleItems = appleAsset.Products;
                _appleMarketPackUrl = appleAsset.StoreUrl;
            }

            if (zarinpalAsset != null)
            {
                _zarinpalItems = zarinpalAsset.Products;
                _zarinpalMarketPackUrl = zarinpalAsset.StoreUrl;
            }

            if (xsollaAsset != null)
            {
                _xsollaItems = xsollaAsset.Products;
                _xsollaMarketPackUrl = xsollaAsset.StoreUrl;
                if (xsollaAsset.SetupConfig != null)
                    _xsollaSetupConfiguration = xsollaAsset.SetupConfig;
                else
                    _xsollaSetupConfiguration = new VendorSetupConfiguration();
            }
        }

        private void OnWizardCreate()
        {
            VendorConfigurationObject googleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(GOOGLE_ASSET_PATH);
            VendorConfigurationObject bazaarAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(BAZAAR_ASSET_PATH);
            VendorConfigurationObject myketAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(MYKET_ASSET_PATH);
            VendorConfigurationObject appleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(APPLE_ASSET_PATH);
            VendorConfigurationObject zarinpalAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(ZARINPAL_ASSET_PATH);
            VendorConfigurationObject xsollaAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(XSOLLA_ASSET_PATH);

            if (googleAsset != null)
            {
                googleAsset.SetProducts(_googleItems);
                googleAsset.SetMarketPackUrl(_googleMarketPackUrl);
                EditorUtility.SetDirty(googleAsset);
            }
            else
            {
                googleAsset = CreateInstance<VendorConfigurationObject>();
                googleAsset.SetProducts(_googleItems);
                googleAsset.SetMarketPackUrl(_googleMarketPackUrl);
                AssetDatabase.CreateAsset(googleAsset, GOOGLE_ASSET_PATH);
            }

            if (bazaarAsset != null)
            {
                bazaarAsset.SetProducts(_bazaarItems);
                bazaarAsset.SetMarketPackUrl(_bazaarMarketPackUrl);
                EditorUtility.SetDirty(bazaarAsset);
            }
            else
            {
                bazaarAsset = CreateInstance<VendorConfigurationObject>();
                bazaarAsset.SetProducts(_bazaarItems);
                bazaarAsset.SetMarketPackUrl(_bazaarMarketPackUrl);
                AssetDatabase.CreateAsset(bazaarAsset, BAZAAR_ASSET_PATH);
            }

            if (myketAsset != null)
            {
                myketAsset.SetProducts(_myketItems);
                myketAsset.SetMarketPackUrl(_myketMarketPackUrl);
                EditorUtility.SetDirty(myketAsset);
            }
            else
            {
                myketAsset = CreateInstance<VendorConfigurationObject>();
                myketAsset.SetProducts(_myketItems);
                myketAsset.SetMarketPackUrl(_myketMarketPackUrl);
                AssetDatabase.CreateAsset(myketAsset, MYKET_ASSET_PATH);
            }

            if (appleAsset != null)
            {
                appleAsset.SetProducts(_appleItems);
                appleAsset.SetMarketPackUrl(_appleMarketPackUrl);
                EditorUtility.SetDirty(appleAsset);
            }
            else
            {
                appleAsset = CreateInstance<VendorConfigurationObject>();
                appleAsset.SetProducts(_appleItems);
                appleAsset.SetMarketPackUrl(_appleMarketPackUrl);
                AssetDatabase.CreateAsset(appleAsset, APPLE_ASSET_PATH);
            }

            if (zarinpalAsset != null)
            {
                zarinpalAsset.SetProducts(_zarinpalItems);
                zarinpalAsset.SetMarketPackUrl(_zarinpalMarketPackUrl);
                EditorUtility.SetDirty(zarinpalAsset);
            }
            else
            {
                zarinpalAsset = CreateInstance<VendorConfigurationObject>();
                zarinpalAsset.SetProducts(_zarinpalItems);
                zarinpalAsset.SetMarketPackUrl(_zarinpalMarketPackUrl);
                AssetDatabase.CreateAsset(zarinpalAsset, ZARINPAL_ASSET_PATH);
            }

            if (xsollaAsset != null)
            {
                xsollaAsset.SetProducts(_xsollaItems);
                xsollaAsset.SetMarketPackUrl(_xsollaMarketPackUrl);
                xsollaAsset.SetSetupConfig(_xsollaSetupConfiguration);
                EditorUtility.SetDirty(xsollaAsset);
            }
            else
            {
                xsollaAsset = CreateInstance<VendorConfigurationObject>();
                xsollaAsset.SetProducts(_xsollaItems);
                xsollaAsset.SetMarketPackUrl(_xsollaMarketPackUrl);
                xsollaAsset.SetSetupConfig(_xsollaSetupConfiguration);
                AssetDatabase.CreateAsset(xsollaAsset, XSOLLA_ASSET_PATH);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

