using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IMarketGroup 
    {
       string DefaultMarketId { get; }
       IEnumerable<IMarketHandler> Markets { get; }
    }
}
