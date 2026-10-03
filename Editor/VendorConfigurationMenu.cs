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
            VendorConfigurationObject googleAsset = FindConfigurationAsset(MarketId.GOOGLE);
            VendorConfigurationObject bazaarAsset = FindConfigurationAsset(MarketId.BAZAAR);
            VendorConfigurationObject myketAsset = FindConfigurationAsset(MarketId.MYKET);
            VendorConfigurationObject appleAsset = FindConfigurationAsset(MarketId.APPLE);
            VendorConfigurationObject zarinpalAsset = FindConfigurationAsset(MarketId.ZARINPAL);
            VendorConfigurationObject xsollaAsset = FindConfigurationAsset(MarketId.XSOLLA);
            if (googleAsset != null)
            {
                _googleItems = googleAsset.Products;
                _googleMarketPackUrl = googleAsset.StoreUrl;
            }

            if (bazaarAsset != null)
            {
                _bazaarItems = bazaarAsset.Products;
                _bazaarMarketPackUrl = bazaarAsset.StoreUrl;
                _bazaarStoreKey = bazaarAsset.StoreKey;
            }

            if (myketAsset != null)
            {
                _myketItems = myketAsset.Products;
                _myketMarketPackUrl = myketAsset.StoreUrl;
                _myketStoreKey = myketAsset.StoreKey;
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
            VendorConfigurationObject googleAsset = FindConfigurationAsset(MarketId.GOOGLE);
            VendorConfigurationObject bazaarAsset = FindConfigurationAsset(MarketId.BAZAAR);
            VendorConfigurationObject myketAsset = FindConfigurationAsset(MarketId.MYKET);
            VendorConfigurationObject appleAsset = FindConfigurationAsset(MarketId.APPLE);
            VendorConfigurationObject zarinpalAsset = FindConfigurationAsset(MarketId.ZARINPAL);
            VendorConfigurationObject xsollaAsset = FindConfigurationAsset(MarketId.XSOLLA);

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
                SetStoreKey(bazaarAsset, _bazaarStoreKey);
                EditorUtility.SetDirty(bazaarAsset);
            }
            else
            {
                bazaarAsset = CreateInstance<VendorConfigurationObject>();
                bazaarAsset.SetProducts(_bazaarItems);
                bazaarAsset.SetMarketPackUrl(_bazaarMarketPackUrl);
                SetStoreKey(bazaarAsset, _bazaarStoreKey);
                AssetDatabase.CreateAsset(bazaarAsset, BAZAAR_ASSET_PATH);
            }

            if (myketAsset != null)
            {
                myketAsset.SetProducts(_myketItems);
                myketAsset.SetMarketPackUrl(_myketMarketPackUrl);
                SetStoreKey(myketAsset, _myketStoreKey);
                EditorUtility.SetDirty(myketAsset);
            }
            else
            {
                myketAsset = CreateInstance<VendorConfigurationObject>();
                myketAsset.SetProducts(_myketItems);
                myketAsset.SetMarketPackUrl(_myketMarketPackUrl);
                SetStoreKey(myketAsset, _myketStoreKey);
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

        /// <summary>
        /// Finds a market's configuration asset wherever it was moved, so the window keeps editing
        /// the same asset after it is moved into a Resources folder. A copy under a Resources folder
        /// is preferred because that is the one VendorDefaultResourceLoader loads.
        /// </summary>
        private static VendorConfigurationObject FindConfigurationAsset(string marketId)
        {
            string fileName = $"{marketId}VendorConfig";
            string foundPath = null;
            foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:{nameof(VendorConfigurationObject)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != fileName)
                    continue;
                if (path.Contains("/Resources/"))
                    return AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(path);
                foundPath ??= path;
            }
            return foundPath != null ? AssetDatabase.LoadAssetAtPath<VendorConfigurationObject>(foundPath) : null;
        }

        private static void SetStoreKey(VendorConfigurationObject asset, string storeKey)
        {
            SerializedObject serializedAsset = new SerializedObject(asset);
            serializedAsset.FindProperty("_setupConfig._storeKey").stringValue = storeKey;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

