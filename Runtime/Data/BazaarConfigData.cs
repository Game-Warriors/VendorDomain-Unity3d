using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [CreateAssetMenu(fileName = RESOURCES_PATH, menuName = "VendorDomain/Bazaar Config Data")]
    public class BazaarConfigData : ScriptableObject
    {
        public const string RESOURCES_PATH = "BazaarConfigData";
        public const string ASSET_PATH = "Assets/AssetData/Resources/BazaarConfigData.asset";

        [SerializeField]
        private string _key;

        public string ApiKey => _key;
    }
}