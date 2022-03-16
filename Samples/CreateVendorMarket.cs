using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Core;

public static class CreateVendorMarket
{
    public static IMarketHandler CreateDefaultMarket()
    {
        IMarketHandler marketHandler = null;
#if BAZAAR && !UNITY_EDITOR
            BazaarHandler tmp = new BazaarHandler();
            marketHandler = tmp;

#elif GOOGLE && !UNITY_EDITOR
            string timeName = TimeZone.CurrentTimeZone.StandardName;
            if (string.Compare(timeName, "+0330") == 0 || string.Compare(timeName, "Iran Standard Time") == 0 || string.Compare(timeName, "IRST") == 0)
            {
                marketHandler = new GameObject("ZarinpalAndroid", typeof(ZarinpalAndroidHandler)).GetComponent<IMarketHandler>();
            }
            else
            {
                marketHandler = new GoogleHandler();
            }
#elif APPLE && !UNITY_EDITOR
            string timeName = TimeZone.CurrentTimeZone.StandardName;
            if (string.Compare(timeName, "+0330") == 0 || string.Compare(timeName, "IRST") == 0)
            {
                marketHandler = new GameObject("ZarinpaliOS", typeof(ZarinpalIosHandler)).GetComponent<IMarketHandler>();
            }
            else
            {
                marketHandler = new AppleHandler();
            }
#else
        marketHandler = new WindowsHandler();
#endif
        return marketHandler;
    }
}
