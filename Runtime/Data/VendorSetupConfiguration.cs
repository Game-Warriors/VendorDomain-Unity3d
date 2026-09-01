using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [System.Serializable]
    public class VendorSetupConfiguration
    {
        [SerializeField]
        private string _storeKey;
        [SerializeField]
        private int _storeId;
        [SerializeField]
        private bool _isTestMode;

        public string StoreKey => _storeKey;
        public int StoreId => _storeId;
        public bool IsTestMode => _isTestMode;
    }
}