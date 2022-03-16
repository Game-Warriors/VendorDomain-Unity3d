using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public struct UnconsumePurchase
    {
        [SerializeField]
        private string purchaseToken;
        [SerializeField]
        private string itemId;
        [SerializeField]
        private long date;

        public string PurchaseToken => purchaseToken;
        public string ItemId => itemId;
        public long Date => date;
    }
}