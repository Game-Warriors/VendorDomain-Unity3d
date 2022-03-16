
namespace GameWarriors.VendorDomian.Data
{
    public class Purchase
    {
        public string orderId;
        public string purchaseToken;
        public string payload;
        public string packageName;
        public int purchaseState;
        public string purchaseTime;
        public string productId;
        public string json;

        public Purchase()
        {

        }

        public Purchase(string id, long time, int state, string payload, string token)
        {
            productId = id;
            purchaseTime = time.ToString();
            purchaseState = state;
            this.payload = payload;
            purchaseToken = token;
        }
    }
}
