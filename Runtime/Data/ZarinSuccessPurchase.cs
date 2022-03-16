using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Data
{
    [Serializable]
    public struct ZarinSuccessPurchase
    {
        [SerializeField]
        private string sku;
        [SerializeField]
        private string authority;
        //[SerializeField]
        //private string refId;
        [SerializeField]
        private long time;
        //[SerializeField]
        //private int price;
        //[SerializeField]
        //private string metaData;

        public string Sku => sku;
        public string Authority => authority;
        //public string Token => refId;

        public DateTime PurchaseTime => DateTime.FromBinary(time);
        //public string MetaData => metaData;
        //public int Price => price;
    }
}
