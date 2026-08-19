using GameWarriors.VendorDomian.Data;
using System;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorResourceLoader
    {
        VendorConfigurationObject Load(string id);
        void LoadAsync(string id, Action<VendorConfigurationObject> onLoadDone);
    }
}