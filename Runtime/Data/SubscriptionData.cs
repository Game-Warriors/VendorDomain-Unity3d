using GameWarriors.VendorDomian.Abstraction;
using System;

namespace GameWarriors.VendorDomian.Data
{
    public readonly struct SubscriptionData : ISubscriptionInfo
    {
        public DateTime ExpireDate { get; }
        public SubscriptionData(DateTime expireDate)
        {
            ExpireDate = expireDate;
        }
    }
}
