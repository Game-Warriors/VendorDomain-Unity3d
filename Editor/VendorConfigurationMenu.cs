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
        private VendorPurchaseItem[] _googleItems;
        [SerializeField]
        private VendorPurchaseItem[] _appleItems;
        [SerializeField]
        private VendorPurchaseItem[] _zarinpalItems;

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
            }

            if (bazaarAsset != null)
            {
                _bazaarItems = bazaarAsset.Products;
            }

            if (appleAsset != null)
            {
                _appleItems = appleAsset.Products;
            }

            if (zarinpalAsset != null)
            {
                _zarinpalItems = zarinpalAsset.Products;
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
                EditorUtility.SetDirty(googleAsset);
            }
            else
            {
                googleAsset = new VendorConfigurationObject(_googleItems);
                AssetDatabase.CreateAsset(googleAsset, GOOGLE_ASSET_PATH);
            }

            if (bazaarAsset != null)
            {
                bazaarAsset.SetProducts(_bazaarItems);
                EditorUtility.SetDirty(bazaarAsset);
            }
            else
            {
                bazaarAsset = new VendorConfigurationObject(_bazaarItems);
                AssetDatabase.CreateAsset(bazaarAsset, BAZAAR_ASSET_PATH);
            }

            if (appleAsset != null)
            {
                appleAsset.SetProducts(_appleItems);
                EditorUtility.SetDirty(appleAsset);
            }
            else
            {
                appleAsset = new VendorConfigurationObject(_appleItems);
                AssetDatabase.CreateAsset(appleAsset, APPLE_ASSET_PATH);
            }

            if (zarinpalAsset != null)
            {
                zarinpalAsset.SetProducts(_zarinpalItems);
                EditorUtility.SetDirty(zarinpalAsset);
            }
            else
            {
                zarinpalAsset = new VendorConfigurationObject(_zarinpalItems);
                AssetDatabase.CreateAsset(zarinpalAsset, ZARINPAL_ASSET_PATH);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

