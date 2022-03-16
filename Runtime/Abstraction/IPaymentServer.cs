using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GameWarriors.VendorDomian.Data;

namespace GameWarriors.VendorDomian.Abstraction
{
    public interface IPaymentServer
    {
        string RequestPayUrl { get; }
        Task<string> GetAuthorizationAsync();
        Task<HttpStatusCode> TryToConsumePayment(string identifier, string purchaseToken, EMarketProvider providerType);
        Task<IList<UnconsumePurchase>> TryGetUnconsumePurchase(string identifier, EMarketProvider providerType);
    }
}
