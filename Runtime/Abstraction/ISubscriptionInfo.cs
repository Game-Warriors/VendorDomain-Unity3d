using System;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface ISubscriptionInfo
    {
        DateTime ExpireDate { get; }
    }
}