using System.Collections.Generic;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IMarketGroup 
    {
       string InitialDefaultMarketId { get; }
       IEnumerable<IMarketHandler> Markets { get; }
    }
}
