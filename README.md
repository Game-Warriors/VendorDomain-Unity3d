# Vendor Domain for Unity

## Table of Contents

<details>
<summary>Contents</summary>

- [Introduction](#introduction)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Store setup](#store-setup)
- [Product configuration](#product-configuration)
- [Integration](#integration)
- [Purchase lifecycle](#purchase-lifecycle)
- [Using the vendor API](#using-the-vendor-api)
- [Subscriptions](#subscriptions)
- [Custom resource loading](#custom-resource-loading)
- [Troubleshooting](#troubleshooting)

</details>

## Introduction

Vendor Domain provides a common Unity API for store initialization, in-app purchases, product metadata, subscriptions, restored transactions, store pages, and rating prompts. Game code communicates with `IVendor` and `IDefaultVendorData`, while platform-specific behavior is implemented by market handlers.

The package currently includes handlers for:

- Google Play
- Apple App Store
- Cafe Bazaar
- Zarinpal on Android and iOS
- Windows and Editor fallback flows

> The package's existing namespace is `GameWarriors.VendorDomian` (including the `Domian` spelling). Use that spelling in imports.

## Features

- One purchasing API across supported markets
- Consumable, non-consumable, and subscription product definitions
- Localized price and product metadata
- Normal and sale product identifiers
- Product bundles containing one or more game currencies
- Store initialization and fetch-state notifications
- Fresh purchase and recovered-unconfirmed-purchase identification
- Transaction receipt and transaction ID forwarding
- Subscription expiration information
- Store-page and native rating operations
- Read-only configuration abstraction through `IVendorConfigurationObject`
- Replaceable configuration loading through `IVendorResourceLoader`

## Requirements

- Unity 2022.3 or newer
- Unity Purchasing 5.x; version 5.4.2 is currently tested
- An `IServiceProvider` containing the services required by the selected handlers
- Store products configured in App Store Connect or Google Play Console with identifiers matching the Unity configuration

For Google Play and Apple App Store, install Unity IAP through Package Manager:

```text
com.unity.purchasing
```

## Installation

Add the package as an embedded package, copy `VendorDomain-Unity3d` into the project, or install it from a Git URL through Unity Package Manager.

[Unity manual: Install a package from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

When installing from a repository that contains several packages, use the package path in the Git URL:

```text
https://github.com/your-org/your-repository.git?path=Assets/VendorDomain-Unity3d
```

## Store setup

### Compilation symbols

Add the relevant custom symbol under **Project Settings > Player > Other Settings > Scripting Define Symbols**:

| Store | Symbol | Handler |
| --- | --- | --- |
| Google Play | `GOOGLE` | `GoogleHandler` |
| Apple App Store | `APPLE` | `AppleHandler` |
| Cafe Bazaar | `BAZAAR` | `BazaarHandler` |

Use platform-specific symbol lists so Android and iOS builds do not include the wrong store handler. The Google and Apple classes are not compiled unless their corresponding symbols are defined.

### Store dashboards

Before testing a purchase:

1. Create each in-app product in Google Play Console or App Store Connect.
2. Use exactly the same product identifier in the Vendor Configuration window.
3. Set the correct product type: `Consumable`, `NonConsumable`, or `Subscription`.
4. Complete the store's banking, tax, agreement, and sandbox-tester setup.
5. Test using a store-installed development build and a sandbox/test account.

## Product configuration

Open **Tools > Vendor Configuration** in the Unity Editor. The window contains separate product arrays and market URL fields for Bazaar, Google, Apple, and Zarinpal.

Each `VendorPurchaseItem` contains:

| Field | Purpose |
| --- | --- |
| Name | Stable application-facing name passed to `IVendor.PurchaseProduct` |
| Product ID | Store SKU configured in the store dashboard |
| Off Product ID | Optional alternative SKU for a sale/discount product |
| Price | Runtime price populated from store metadata |
| Items Data | Currencies or rewards granted by the product |
| Type | Consumable, non-consumable, or subscription |
| Purchase Limit | Optional application-level purchase restriction |
| Is Enable | Application-level product availability |

Press **Save** to update or create the configuration assets under:

```text
Assets/AssetData/Vendor
```

The expected asset names are based on `MarketId`:

```text
GooglePlayVendorConfig.asset
AppleVendorConfig.asset
BazaarVendorConfig.asset
ZarinpalVendorConfig.asset
```

### Important: default loader location

`VendorDefaultResourceLoader` uses `Resources.Load`. If you use this loader, place the generated assets under any Unity `Resources` directory while preserving their filenames. For example:

```text
Assets/Resources/GooglePlayVendorConfig.asset
Assets/Resources/AppleVendorConfig.asset
```

If your project uses Addressables, Asset Bundles, or another content system, keep the assets in its required location and provide a custom `IVendorResourceLoader`.

## Integration

Integration requires:

1. An `IVendorResourceLoader`
2. An `IVendorEventListener`
3. An `IMarketGroup` containing the desired handler
4. A service provider that exposes the listener and loader
5. A `VendorSystem` initialized after its configuration has loaded

### Create a market group

```csharp
using System;
using System.Collections.Generic;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Core;

public sealed class GameMarketGroup : IMarketGroup
{
    public string InitialDefaultMarketId { get; }
    public IEnumerable<IMarketHandler> Markets { get; }

    public GameMarketGroup(IVendorResourceLoader resourceLoader)
    {
#if GOOGLE
        InitialDefaultMarketId = MarketId.GOOGLE;
        Markets = new IMarketHandler[]
        {
            new GoogleHandler(resourceLoader)
        };
#elif APPLE
        InitialDefaultMarketId = MarketId.APPLE;
        Markets = new IMarketHandler[]
        {
            new AppleHandler(resourceLoader)
        };
#else
        throw new PlatformNotSupportedException(
            "Configure a market handler for the current platform.");
#endif
    }
}
```

### Implement purchase events

`PurchasedSuccessful` means that the store has paid the order and it is ready to be granted or validated. Use `transactionId` as an idempotency key so the same transaction can never grant rewards twice.

```csharp
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Data;
using GameWarriors.VendorDomian.Enums;
using UnityEngine;

public sealed class GameVendorEvents : IVendorEventListener
{
    public void PurchasedSuccessful(
        string marketId,
        VendorPurchaseItem purchaseItem,
        string currencyType,
        long purchaseTime,
        string receipt,
        string transactionId,
        EPurchaseOrigin purchaseOrigin)
    {
        switch (purchaseOrigin)
        {
            case EPurchaseOrigin.FreshPurchase:
                // A transaction completed during the active purchase flow.
                break;

            case EPurchaseOrigin.RecoveredUnconfirmedPurchase:
                // An unfinished transaction was recovered by FetchPurchases.
                // Check transactionId before granting it again.
                break;
        }

        // Validate/persist the receipt, then grant purchaseItem.CurrenciesData.
    }

    public void ConsumeSuccess(string marketId, VendorPurchaseItem item,
        string receipt, string transactionId) { }

    public void ConsumeFailed(string marketId, VendorPurchaseItem item,
        string receipt, string transactionId) { }

    public void PurchasedFailed(string marketId, VendorPurchaseItem item,
        int state, string error) => Debug.LogError(error);

    public void UserCancelPurchase(string marketId, VendorPurchaseItem item,
        string error) { }

    public void StoreInitializeFailed(string marketId, string error) =>
        Debug.LogError(error);

    public void OnError(string marketId, int state, string error) =>
        Debug.LogError(error);

    public void OnVendorStateChanged(string marketId,
        EStoreSetupState setupState) { }

    public void OnPurchaseItemsUpdate(string marketId) { }
    public void OnSubscriptionsUpdate(string marketId) { }
}
```

### Build the vendor system

The example below uses `ServiceProvider` from the Game Warriors Dependency Injection package. Any `IServiceProvider` implementation is valid if it returns the registered objects from `GetService(Type)`.

```csharp
using GameWarriors.DependencyInjection.Core;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Core;
using UnityEngine;

public sealed class VendorStartup : MonoBehaviour
{
    public IVendor Vendor { get; private set; }
    public IDefaultVendorData VendorData { get; private set; }

    private async void Awake()
    {
        var provider = new ServiceProvider();
        var loader = new VendorDefaultResourceLoader();
        var listener = new GameVendorEvents();
        var marketGroup = new GameMarketGroup(loader);

        provider.SetSingletonService(typeof(IVendorResourceLoader), loader);
        provider.SetSingletonService(typeof(IVendorEventListener), listener);
        provider.SetSingletonService(typeof(IMarketGroup), marketGroup);

        var vendorSystem = new VendorSystem(provider, marketGroup);
        await vendorSystem.WaitForLoading();
        vendorSystem.Initialization();

        Vendor = vendorSystem;
        VendorData = vendorSystem;
    }
}
```

Do not allow purchase buttons until all three readiness values are true:

```csharp
bool canPurchase = vendor.IsInitialized
                   && vendor.IsProductFetched
                   && vendor.IsPurchasesFetched;
```

You can also react to `OnVendorStateChanged` instead of polling.

## Purchase lifecycle

Google and Apple follow this lifecycle:

```text
Connect to store
    -> Fetch products
    -> Fetch existing purchases
    -> Ready

PurchaseProduct
    -> PurchasedSuccessful(FreshPurchase)
    -> Confirm purchase
    -> ConsumeSuccess

FetchPurchases with an unfinished order
    -> PurchasedSuccessful(RecoveredUnconfirmedPurchase)
    -> Confirm purchase
    -> ConsumeSuccess
```

The handlers disable Unity IAP's automatic rerouting of fetched pending orders. This lets them reliably distinguish a new purchase from an unfinished transaction returned by `FetchPurchases()`.

`RecoveredUnconfirmedPurchase` does not mean a restored entitlement. Restored non-consumables and subscriptions normally appear as confirmed orders. It means that the store returned an order that had not previously been confirmed.

## Using the vendor API

### List products

```csharp
foreach (VendorPurchaseItem item in vendorData.PurchaseItems)
{
    Debug.Log($"{item.Name}: {item.ItemMeta?.LocalisedPrice}");
}
```

Product metadata is available after `IsProductFetched` becomes `true`.

### Purchase a product

Use the configured product `Name`, not its store SKU:

```csharp
vendor.PurchaseProduct("starter_pack", hasOff: false);
```

To select the configured sale SKU:

```csharp
vendorData.EnableProductOffState("starter_pack");
vendor.PurchaseProduct("starter_pack", hasOff: true);
```

### Recover unfinished purchases

```csharp
vendor.CheckUnconsumePurchase();
```

Recovered pending transactions are reported through `PurchasedSuccessful` with `EPurchaseOrigin.RecoveredUnconfirmedPurchase`.

### Open the store or rating prompt

```csharp
vendor.OpenVendorLocation();

vendor.OpenRate(success =>
{
    Debug.Log("Rating request opened: " + success);
});
```

## Subscriptions

After purchases have been fetched, query subscription information using the configured product name:

```csharp
ISubscriptionInfo subscription =
    vendorData.GetSubscriptionInfo("premium_subscription");

if (subscription != null)
{
    Debug.Log(subscription.ExpireDate);
}
```

Refresh subscription UI when `OnSubscriptionsUpdate` is invoked.

## Custom resource loading

Handlers depend on `IVendorConfigurationObject`, not directly on Unity's concrete `VendorConfigurationObject`. A custom loader can therefore obtain configuration through another asset system while keeping handlers unchanged.

```csharp
using System;
using GameWarriors.VendorDomian.Abstraction;

public sealed class CustomVendorResourceLoader : IVendorResourceLoader
{
    public IVendorConfigurationObject Load(string marketId)
    {
        // Return the configuration associated with marketId.
        throw new NotImplementedException();
    }

    public void LoadAsync(string marketId,
        Action<IVendorConfigurationObject> onLoadDone)
    {
        // Load asynchronously, then invoke onLoadDone(configuration).
        throw new NotImplementedException();
    }
}
```

Configuration IDs must match the handler's `Id`, such as `GooglePlay` or `Apple`.

## Troubleshooting

### Handler type cannot be found

Add the required `GOOGLE`, `APPLE`, or `BAZAAR` scripting symbol and allow Unity to recompile.

### Configuration is null

When using `VendorDefaultResourceLoader`, verify that the asset is under a `Resources` directory and has the exact expected filename, for example `AppleVendorConfig.asset`.

### Products are not returned by the store

- Confirm that the SKU and product type match the store dashboard.
- Confirm that the product is active and available to the tester.
- Use a build installed through the store's testing track where required.
- Verify agreements and banking information in the store dashboard.

### A recovered purchase grants rewards twice

Persist every successful `transactionId` and check it before granting rewards. Treat purchase callbacks as retryable delivery notifications.

### Purchase buttons are used too early

Wait for initialization, products, and purchases to finish, or handle the corresponding `EStoreSetupState` notifications.

### Apple restore behavior

Call the handler's `RefreshPurchases` flow from an explicit **Restore Purchases** button where required by the App Store. The restore operation fetches purchases and updates subscriptions after completion.
