using System;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IVendorResourceLoader
    {
        IVendorConfigurationObject Load(string id);
        void LoadAsync(string id, Action<IVendorConfigurationObject> onLoadDone);
    }
}
