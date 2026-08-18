using GameWarriors.VendorDomian.Data;
using System;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorResourceLoader
    {
        void LoadAsync(string id, Action<VendorConfigurationObject> onLoadDone);
    }
}