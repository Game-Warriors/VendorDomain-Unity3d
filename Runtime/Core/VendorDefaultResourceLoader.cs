using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using System;
using UnityEngine;

namespace GameWarriors.VendorDomian.Core
{
    public class VendorDefaultResourceLoader : IVendorResourceLoader
    {
        public void LoadAsync(string id, Action<VendorConfigurationObject> onLoadDone)
        {
            ResourceRequest operation = Resources.LoadAsync<VendorConfigurationObject>($"{id}VendorConfig");
            operation.completed += input =>
            {
                var tmp = input as ResourceRequest;
                onLoadDone(tmp.asset as VendorConfigurationObject);
                Resources.UnloadAsset(tmp.asset);
            };
        }
    }
}
