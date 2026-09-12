    version : 0.3.3
    add build tools helpers for: myket,bazaar and xsolla, add usage doc in readme

    version : 0.3.2
    extract the shared Unity IAP logic of the Google, Apple, and Xsolla handlers into the new UnityIapMarketHandlerBase class. The base class owns the store lifecycle, product and subscription tables, order handling, and all store controller callbacks, so each market only provides its store controller, store links, configuration fields, and purchase refresh behavior. Align the three handlers on the corrected behavior: unsubscribe store events on dispose, register off product ids in the sku table, guard duplicate and null subscription info, and report the pending order receipt when recovering a duplicate purchase failure.

    version : 0.3.1
    update Google, Apple, and Xsolla for the Unity IAP 5.4 lifecycle. Add deferred-purchase notifications, store-disconnection and purchase-fetch failure handling, and retry-safe confirmation outcomes. Fix Xsolla store-controller initialization and fetch guards.

    version : 0.3.0
    fix myket handler get item by name bug.

    version : 0.2.9
    improvement on handlers initialization state.

    version : 0.2.8
    some fix on iOS handler.

    version : 0.2.7
    apply fetch purchase check state in google and apple handler.

    version : 0.2.6
    fix bazaar get sku bug.

    version : 0.2.5
    add new single provider vendor system.

    version : 0.2.4
    add editor feature for myket and xsolla, develop xsolla handler.

    version : 0.2.3
    import new myket sdk and setup, develop myket handler.

    version : 0.2.2
    import new popoolakey sdk and setup, develop bazaar handler.

    version : 0.2.1
    fix duplicate purchase bug.

    version : 0.2.0
    add logs for debuging, fix some naming.

    version : 0.1.9
    reimplement pending purchase structure. break dependency to VendorPurchaseItem class to other abstraction dependency.

    version : 0.1.8
    refactor Apple IAP handler for Unity Purchasing 5.4.2, fix Google store state notifications, add market package URL editing, decouple handlers from the concrete vendor configuration asset, and identify fresh and recovered unconfirmed purchases

    version : 0.1.7
    fix circle reference issue 

    version : 0.1.6
    fix editor save location and data name, fix reconnect on try to purchase

    version : 0.1.5
    add product meta data structure and subscription info structure

    version : 0.1.4
    add refresh product methods, fetch products, purchases states to vendors

    version : 0.1.3
    refactore and restrcuture vendor system and upgrade unity3d purchase package to just support version 5 to upper
