using UnityEngine;
using UnityEditor;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Constants;
using System.IO;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public class VendorConfigurationMenu : ScriptableWizard
    {
        private const string ASSET_DIRECTORY = "Assets/AssetData/Vendor";
        private readonly string BAZAAR_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.BAZAAR}VendorConfig.asset";
        private readonly string GOOGLE_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.GOOGLE}VendorConfig.asset";
        private readonly string APPLE_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.APPLE}VendorConfig.asset";
        private readonly string ZARINPAL_ASSET_PATH = $"{ASSET_DIRECTORY}/{MarketId.ZARINPAL}VendorConfig.asset";

        [SerializeField]
        private VendorPurchaseItem[] _bazaarItems;
        [SerializeField]
        private string _bazaarMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _googleItems;
        [SerializeField]
        private string _googleMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _appleItems;
        [SerializeField]
        private string _appleMarketPackUrl;
        [SerializeField]
        private VendorPurchaseItem[] _zarinpalItems;
        [SerializeField]
        private string _zarinpalMarketPackUrl;

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
            VendorConfigurationObject appleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(APPLE_ASSET_PATH);
            VendorConfigurationObject zarinpalAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(ZARINPAL_ASSET_PATH);
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
        }

        private void OnWizardCreate()
        {
            VendorConfigurationObject googleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(GOOGLE_ASSET_PATH);
            VendorConfigurationObject bazaarAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(BAZAAR_ASSET_PATH);
            VendorConfigurationObject appleAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(APPLE_ASSET_PATH);
            VendorConfigurationObject zarinpalAsset = AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(ZARINPAL_ASSET_PATH);

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
            AssetDatabase.SaveAssets();
        }
    }
}

