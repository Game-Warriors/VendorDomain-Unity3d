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
- [Single market setup](#single-market-setup)
- [Purchase lifecycle](#purchase-lifecycle)
- [Using the vendor API](#using-the-vendor-api)
- [Subscriptions](#subscriptions)
- [Custom resource loading](#custom-resource-loading)
- [Troubleshooting](#troubleshooting)
- [Editor build tools](#editor-build-tools)

</details>

## Introduction

Vendor Domain provides a common Unity API for store initialization, in-app purchases, product metadata, subscriptions, restored transactions, store pages, and rating prompts. Game code communicates with `IVendor` and `IDefaultVendorData`, while platform-specific behavior is implemented by market handlers.

The package currently includes handlers for:

- Google Play
- Apple App Store
- Cafe Bazaar
- Myket
- Xsolla
- Zarinpal on Android and iOS
- Windows and Editor fallback flows

> The package's existing namespace is `GameWarriors.VendorDomian` (including the `Domian` spelling). Use that spelling in imports.
> **Note:** The Google, Apple, and Xsolla handlers require Unity IAP 5.4.2 or newer.
## Features

- One purchasing API across supported markets
- Consumable, non-consumable, and subscription product definitions
- Localized price and product metadata
- Normal and sale product identifiers
- Product bundles containing one or more game currencies
- Store initialization and fetch-state notifications
- Fresh purchase and recovered-unconfirmed-purchase identification
- Deferred purchase notifications through `PurchasedDelayed`
- Transaction receipt and transaction ID forwarding
- Store-disconnection and product/purchase-fetch failure reporting
- Retry-safe purchase confirmation with distinct consume success and failure events
- Subscription expiration information
- Store-page and native rating operations
- Read-only configuration abstraction through `IVendorConfigurationObject`
- Replaceable configuration loading through `IVendorResourceLoader`

## Requirements

- Unity 2022.3 or newer
- Unity Purchasing 5.x; version 5.4.2 is currently tested
- An `IServiceProvider` containing the services required by the selected handlers
- Store products configured in App Store Connect or Google Play Console with identifiers matching the Unity configuration
- To use the Bazaar handler, also import the [Poolakey SDK package](https://github.com/Game-Warriors/Poolakey-sdk).
- To use the Myket handler, also import the [Myket SDK package](https://github.com/Game-Warriors/Myket-sdk).
- To use the Xsolla handler, also import the [Xsolla SDK package](https://github.com/Game-Warriors/Xsolla-unity3d).

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
| Myket | `MYKET` | `MyketHandler` |
| Xsolla | `XSOLLA` | `XsollaHandler` |

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
3. A market handler, supplied either directly or through an optional `IMarketGroup`
4. A service provider that exposes the listener and loader
5. A vendor system initialized after its configuration has loaded

Two vendor systems are available, and they differ only in how step 3 is provided:

| System | Market source | Use when |
| --- | --- | --- |
| `SingleProviderVendorSystem` | one `IMarketHandler` | the build targets a single store |
| `VendorSystem` | an `IMarketGroup` | the build has to hold more than one store, or switch the active one at runtime |

`IMarketGroup` is optional. If a build ships with a single store, skip it and pass the handler directly; see [Single market setup](#single-market-setup). The rest of this section describes the market group route.

### Create a market group (optional)

`IMarketGroup` is a small read-only description of the markets a build carries:

```csharp
public interface IMarketGroup
{
    string InitialDefaultMarketId { get; }
    IEnumerable<IMarketHandler> Markets { get; }
}
```

`Markets` holds every handler the build can use, and `InitialDefaultMarketId` selects which one is active at startup. If no market in the group matches that id, `VendorSystem` falls back to the first market in `Markets`. Implement it only when a build genuinely carries more than one store, for example a Google Play build that can also sell through Xsolla:

```csharp
using System.Collections.Generic;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Constants;
using GameWarriors.VendorDomian.Core;

public sealed class MultiMarketGroup : IMarketGroup
{
    public string InitialDefaultMarketId { get; }
    public IEnumerable<IMarketHandler> Markets { get; }

    public MultiMarketGroup(IVendorResourceLoader resourceLoader)
    {
        InitialDefaultMarketId = MarketId.GOOGLE;
        Markets = new IMarketHandler[]
        {
            new GoogleHandler(resourceLoader),
            new XsollaHandler(resourceLoader)
        };
    }
}
```

Each market in the group needs its own configuration asset, named after its `MarketId`, and every handler in the group must have its scripting define symbol set in that build.

A group can also carry exactly one market per platform, which keeps the `VendorSystem` API available without maintaining a second store:

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

### Use a market group

Register the group in the service provider and pass it to `VendorSystem`:

```csharp
var marketGroup = new MultiMarketGroup(loader);
provider.SetSingletonService(typeof(IMarketGroup), marketGroup);

var vendorSystem = new VendorSystem(provider, marketGroup);
await vendorSystem.WaitForLoading();
vendorSystem.Initialization();
```

`VendorSystem` then treats the group as follows:

| Call | Effect on the group |
| --- | --- |
| `WaitForLoading` / `WaitForLoadingCoroutine` | calls `StartLoading` on every market and waits until none of them reports `IsLoading` |
| `Initialization` | initializes every market in the group, so each handler connects to its own store |
| `IVendor.ChangeDefaultMarket(id)` | makes the market with that id the active one |
| every other `IVendor` and `IDefaultVendorData` member | is forwarded to the active market only |

`IDefaultVendorData.MarketId` reports which market is currently active, which is useful after a switch:

```csharp
vendor.ChangeDefaultMarket(MarketId.XSOLLA);
Debug.Log(vendorData.MarketId);
```

Because every market is initialized, a group with several stores connects to all of them at startup and each one needs a valid configuration asset. Keep the group to the markets a build actually sells through.

> **Note:** `VendorSystem.ChangeDefaultMarket` currently only compares the first market in the group before returning, so switching is reliable only when the requested market happens to be the first one registered.

### Implement purchase events

`PurchasedSuccessful` means that the store has paid the order and it is ready to be granted or validated. Use `transactionId` as an idempotency key so the same transaction can never grant rewards twice.

```csharp
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Enums;
using UnityEngine;

public sealed class GameVendorEvents : IVendorEventListener
{
    public void PurchasedSuccessful(
        string marketId,
        IProductItem purchaseItem,
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
        // After fulfillment succeeds, call IVendor.ConsumePurchase(transactionId).
    }

    public void PurchasedDelayed(
        string marketId,
        IProductItem purchaseItem,
        string currencyType,
        long purchaseTime,
        string receipt,
        string transactionId,
        EPurchaseOrigin purchaseOrigin)
    {
        // Payment or approval is still pending. Do not grant or confirm it.
        // Deferred orders can have an empty receipt and transactionId.
    }

    public void ConsumeSuccess(string marketId, IProductItem item,
        string receipt, string transactionId) { }

    public void ConsumeFailed(string marketId, IProductItem item,
        string receipt, string transactionId)
    {
        // Confirmation failed. The pending order remains available for retry.
    }

    public void PurchasedFailed(string marketId, IProductItem item,
        int state, string error) => Debug.LogError(error);

    public void UserCancelPurchase(string marketId, IProductItem item,
        string error) { }

    public void StoreInitializeFailed(string marketId, string error) =>
        Debug.LogError(error);

    public void OnError(string marketId, int state, string error) =>
        Debug.LogError(error);

    public void OnVendorStateChanged(string marketId,
        EStoreSetupState setupState) { }

    public void OnProductItemsUpdate(string marketId) { }
    public void OnSubscriptionsUpdate(string marketId) { }
}
```

### Build the vendor system with a market group

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

## Single market setup

Most builds target one store at a time, selected by a scripting define symbol. `SingleProviderVendorSystem` covers that case: it implements the same `IVendor` and `IDefaultVendorData` API as `VendorSystem`, but takes an `IMarketHandler` directly, so no `IMarketGroup` implementation is required.

```csharp
public SingleProviderVendorSystem(IServiceProvider serviceProvider, IMarketHandler marketHandler)
```

### Build the vendor system with one handler

```csharp
using System;
using GameWarriors.DependencyInjection.Core;
using GameWarriors.VendorDomian.Abstraction;
using GameWarriors.VendorDomian.Core;
using UnityEngine;

public sealed class SingleVendorStartup : MonoBehaviour
{
    public IVendor Vendor { get; private set; }
    public IDefaultVendorData VendorData { get; private set; }

    private async void Awake()
    {
        var provider = new ServiceProvider();
        var loader = new VendorDefaultResourceLoader();
        var listener = new GameVendorEvents();

        provider.SetSingletonService(typeof(IVendorResourceLoader), loader);
        provider.SetSingletonService(typeof(IVendorEventListener), listener);

        IMarketHandler marketHandler = CreateMarketHandler(loader);

        var vendorSystem = new SingleProviderVendorSystem(provider, marketHandler);
        await vendorSystem.WaitForLoading();
        vendorSystem.Initialization();

        Vendor = vendorSystem;
        VendorData = vendorSystem;
    }

    private static IMarketHandler CreateMarketHandler(IVendorResourceLoader loader)
    {
#if GOOGLE
        return new GoogleHandler(loader);
#elif APPLE
        return new AppleHandler(loader);
#elif XSOLLA
        return new XsollaHandler(loader);
#else
        throw new PlatformNotSupportedException(
            "Configure a market handler for the current platform.");
#endif
    }
}
```

`IMarketGroup` is not registered in the service provider, but `IVendorResourceLoader` and `IVendorEventListener` still are: `WaitForLoading` resolves the loader, and the handler resolves the listener during `Initialization`.

### Waiting without async/await

`SingleProviderVendorSystem` also exposes a coroutine variant of the loading wait, for startup flows that are not `async`:

```csharp
private IEnumerator Start()
{
    yield return vendorSystem.WaitForLoadingCoroutine();
    vendorSystem.Initialization();
}
```

Both variants call `StartLoading` on the handler and then wait until `IsLoading` becomes `false`. Call `Initialization()` only after the wait completes, so the configured products are available to the store connection.

### Differences from `VendorSystem`

| Topic | `VendorSystem` | `SingleProviderVendorSystem` |
| --- | --- | --- |
| Constructor dependency | `IMarketGroup` | a single `IMarketHandler` |
| Active market | `InitialDefaultMarketId`, or the first market in the group | the handler passed to the constructor |
| `IVendor.ChangeDefaultMarket` | switches the active market | throws `NotSupportedException` |
| Loading wait | waits for every market in the group | waits for the single handler |
| `IDefaultVendorData.MarketId` | id of the active market | id of the handler |

Everything else behaves identically: readiness flags, purchase flow, consumption, subscriptions, store page, and rating calls are forwarded to the single handler.

```csharp
bool canPurchase = vendor.IsInitialized
                   && vendor.IsProductFetched
                   && vendor.IsPurchasesFetched;
```

Because `ChangeDefaultMarket` is not supported, do not expose a market-switching UI when this system is used. If a build has to select between stores at runtime, use `VendorSystem` with an `IMarketGroup` instead.

## Purchase lifecycle

Google, Apple, and Xsolla use the Unity IAP 5.4 order lifecycle:

```text
Connect to store
    -> Fetch products
    -> Fetch existing purchases
    -> Ready

PurchaseProduct
    -> PendingOrder
        -> PurchasedSuccessful(FreshPurchase)
        -> Validate, persist, and grant idempotently
        -> ConsumePurchase(transactionId)
            -> ConfirmedOrder -> ConsumeSuccess and remove pending order
            -> FailedOrder -> ConsumeFailed and retain pending order for retry
    -> DeferredOrder
        -> PurchasedDelayed (do not grant or confirm)
    -> FailedOrder
        -> PurchasedFailed or UserCancelPurchase

FetchPurchases with an unfinished order
    -> PurchasedSuccessful(RecoveredUnconfirmedPurchase)
    -> Validate/persist idempotently
    -> ConsumePurchase(transactionId)
```

The handlers disable Unity IAP's automatic rerouting of fetched pending orders. This lets them reliably distinguish a new purchase from an unfinished transaction returned by `FetchPurchases()`.

`RecoveredUnconfirmedPurchase` does not mean a restored entitlement. Restored non-consumables and subscriptions normally appear as confirmed orders. It means that the store returned an order that had not previously been confirmed.

`ConsumePurchase` returns `true` when confirmation starts. It returns `false` when the transaction is unknown, the handler has no store controller, or confirmation for the same transaction is already in progress. A confirmation failure leaves the order pending so it can be retried safely.

Store disconnections and purchase-fetch failures reset their operation state and are reported through `OnError`. Deferred purchases are reported separately through `PurchasedDelayed`; they are not purchase or consumption failures.

## Using the vendor API

### List products

```csharp
foreach (IProductItem item in vendorData.PurchaseItems)
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

### Purchase confirmation fails

Handle `ConsumeFailed` as a retryable acknowledgement failure. The handler retains the pending order, so call `ConsumePurchase(transactionId)` again after connectivity returns. Repeated calls while the same confirmation is already running return `false`.

### A purchase is delayed

Handle `PurchasedDelayed` by informing the player that payment or approval is pending. Do not grant content or call `ConsumePurchase` until the handler later reports `PurchasedSuccessful`.

### Purchase buttons are used too early

Wait for initialization, products, and purchases to finish, or handle the corresponding `EStoreSetupState` notifications.

### Apple restore behavior

Call the handler's `RefreshPurchases` flow from an explicit **Restore Purchases** button where required by the App Store. The restore operation fetches purchases and updates subscriptions after completion.

## Editor build tools

`Assets/VendorDomain-Unity3d/Editor/BuildTools` contains editor-only static helper classes that prepare an Android build for one specific store before Unity exports the Gradle project. They edit the Unity Gradle templates and toggle native plugin platform compatibility, so a single project can be built for several stores without keeping every store's plugins and dependencies permanently enabled.

All of them live in the `GameWarriors.VendorDomian.VendorEditor` namespace and are plain static classes, so they can be called from a menu item, a `ScriptableObject` inspector, or a batch-mode build script.

| Class | Responsibility |
| --- | --- |
| `VendorBuildTools` | Enables or disables a native plugin for a build target |
| `BazaarBuildTools` | Toggles the Poolakey plugin and cleans Bazaar Gradle dependencies |
| `MyketBuildTools` | Writes the Myket manifest placeholders, IAB public key, and Gradle dependency |
| `XsollaBuildTools` | Toggles the Xsolla native source and its Gradle dependency |

### Build tool requirements

These tools operate on the Gradle templates generated by Unity, so enable them first under **Project Settings > Player > Publishing Settings**:

- **Custom Main Gradle Template** &rarr; `Assets/Plugins/Android/mainTemplate.gradle`
- **Custom Launcher Gradle Template** &rarr; `Assets/Plugins/Android/launcherTemplate.gradle`

Dependencies are injected immediately before the `**DEPS**` marker in `mainTemplate.gradle`. If the marker is missing, the lines are appended to the end of the file instead. When a template file does not exist, the call logs a warning and returns without changing anything.

Because these classes live under an `Editor` folder they are not compiled into a player build. Call them from editor code only, and run them **before** starting the build.

### `VendorBuildTools`

```csharp
public static void SetEnabled(
    string packagePath,
    bool enabled,
    BuildTarget buildTarget = BuildTarget.Android)
```

Loads the `PluginImporter` at `packagePath` and marks the plugin compatible or incompatible with `buildTarget`. `packagePath` is a project-relative asset path, so it can point either inside `Assets` or inside an embedded/UPM package under `Packages`. A missing importer only logs a warning, which makes the call safe to run in a project that does not contain that store's SDK.

```csharp
VendorBuildTools.SetEnabled("Assets/Plugins/Android/poolakey-release.aar", false);
VendorBuildTools.SetEnabled("Assets/Plugins/iOS/SomeLibrary.a", true, BuildTarget.iOS);
```

The other three classes use this method for their plugin toggling, so it is the only place that needs changing if a plugin path or platform rule moves.

### `BazaarBuildTools`

```csharp
public static void SetupMainTemplateForBazaar(
    bool useBazaar,
    string poolakeyVersion = "2.0.0",
    string kotlinVersion = "1.4.20")
```

Prepares an Android build for Cafe Bazaar:

1. Enables or disables `poolakey-release.aar` in both supported locations, `Packages/com.gamewarriors.bazaar/Plugins/` and `Assets/Plugins/Android/`.
2. Removes any existing Poolakey and Kotlin `stdlib-jdk7` dependency lines matching the given versions from `mainTemplate.gradle`.
3. Rewrites the template.

```csharp
BazaarBuildTools.SetupMainTemplateForBazaar(useBazaar: true);
```

> **Note:** the Gradle dependency-injection branch is currently disabled in code (`if (useBazaar && false)`), so this method only cleans the template and toggles the AAR. Bazaar support is expected to ship through the bundled `poolakey-release.aar` rather than a remote Maven dependency. If a project needs the Maven coordinates as well, add them to `mainTemplate.gradle` manually or re-enable that branch.

### `MyketBuildTools`

Myket needs both a launcher-template change (manifest placeholders and the IAB public key) and a main-template change (the billing library).

```csharp
public static void SetupMyketIabPublicKey(bool useMyket, string myketIabPublicKey)
public static void SetupMyketIabPublicKey(bool useMyket, string myketIabPublicKey, string launcherGradle)

public static void SetupMainTemplateForMyket(bool useMyket)
public static void SetupMainTemplateForMyket(bool useMyket, string mainGradle, string billingVersion = "unity-1.6")
```

The short overloads resolve the templates under `Application.dataPath + "/Plugins/Android"`. The overloads that take a path accept an absolute template path, which is useful for tests or for a project that keeps its templates elsewhere.

`SetupMyketIabPublicKey` rewrites `launcherTemplate.gradle`:

1. Removes any previous `buildConfigField` line containing `IAB_PUBLIC_KEY`.
2. Removes the previous `manifestPlaceholders` block containing `marketApplicationId`.
3. Inserts a fresh `manifestPlaceholders` line after `applicationId`, holding `marketApplicationId`, `marketBindAddress`, and `marketPermission`. When `useMyket` is `false` these are written as empty strings, which clears the Myket billing service and permission from the merged manifest.
4. When `useMyket` is `true` and a key is supplied, inserts `buildConfigField "String", "IAB_PUBLIC_KEY", "\"<key>\""` so the native billing layer can verify purchase signatures.

The method is idempotent: running it repeatedly replaces the previous values instead of stacking them. If `applicationId` is not found in the template, it logs an error and makes no change.

`SetupMainTemplateForMyket` removes any existing `com.github.myketstore:myket-billing-unity:<billingVersion>` line and, when `useMyket` is `true`, injects it again before the `**DEPS**` marker.

```csharp
MyketBuildTools.SetupMyketIabPublicKey(useMyket: true, MyketKeys.IabPublicKey);
MyketBuildTools.SetupMainTemplateForMyket(useMyket: true);
```

Keep the public key out of source control where the project requires it: pass it from an environment variable or a local build settings asset instead of hard-coding it in the menu item.

### `XsollaBuildTools`

```csharp
public static void SetupMainTemplateForXsolla(
    bool isXsollaEnable,
    string xsollaVersion = "3.0.461")
```

Toggles `Packages/com.xsolla.sdk/Plugins/XsollaSDK/Android/XsollaStoreClientNativeAndroid.java` for Android, then removes every `com.xsolla.android:mobile` dependency line from `mainTemplate.gradle` and re-adds `implementation 'com.xsolla.android:mobile:<xsollaVersion>'` when the store is enabled. Because removal matches the coordinate without its version, changing SDK versions does not leave a stale line behind.

```csharp
XsollaBuildTools.SetupMainTemplateForXsolla(isXsollaEnable: true, "3.0.461");
```

### Selecting a store for a build

The build tools change project files, not scripting symbols, so pair them with the [compilation symbols](#compilation-symbols) that select the matching handler. A single entry point keeps both in sync:

```csharp
using GameWarriors.VendorDomian.VendorEditor;
using UnityEditor;
using UnityEditor.Build;

public static class StoreBuildSetup
{
    public enum EStoreTarget { Google, Bazaar, Myket, Xsolla }

    [MenuItem("Tools/Vendor/Prepare Android Build/Google Play")]
    public static void PrepareGoogle() => Prepare(EStoreTarget.Google);

    [MenuItem("Tools/Vendor/Prepare Android Build/Bazaar")]
    public static void PrepareBazaar() => Prepare(EStoreTarget.Bazaar);

    [MenuItem("Tools/Vendor/Prepare Android Build/Myket")]
    public static void PrepareMyket() => Prepare(EStoreTarget.Myket);

    [MenuItem("Tools/Vendor/Prepare Android Build/Xsolla")]
    public static void PrepareXsolla() => Prepare(EStoreTarget.Xsolla);

    public static void Prepare(EStoreTarget target)
    {
        BazaarBuildTools.SetupMainTemplateForBazaar(target == EStoreTarget.Bazaar);

        bool useMyket = target == EStoreTarget.Myket;
        MyketBuildTools.SetupMainTemplateForMyket(useMyket);
        MyketBuildTools.SetupMyketIabPublicKey(useMyket, MyketKeys.IabPublicKey);

        XsollaBuildTools.SetupMainTemplateForXsolla(target == EStoreTarget.Xsolla);

        string symbol = target switch
        {
            EStoreTarget.Bazaar => "BAZAAR",
            EStoreTarget.Myket => "MYKET",
            EStoreTarget.Xsolla => "XSOLLA",
            _ => "GOOGLE"
        };
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, symbol);
        AssetDatabase.Refresh();
    }
}
```

Always call every store's setup method with the correct `bool`, including the stores that are not being built. Each method only cleans up its own dependency and plugin state when it is passed `false`, so a skipped call can leave a previous store's plugin or Gradle dependency in the build.

For a command-line build, run the preparation step first and let Unity recompile before `BuildPipeline.BuildPlayer` runs:

```text
Unity -quit -batchmode -projectPath . -executeMethod StoreBuildSetup.PrepareMyket
Unity -quit -batchmode -projectPath . -executeMethod GameBuild.BuildAndroid
```

### Build tool troubleshooting

| Symptom | Cause |
| --- | --- |
| `mainTemplate.gradle not found` warning | The custom main Gradle template is not enabled in Publishing Settings |
| `launcherTemplate.gradle not found` warning | The custom launcher Gradle template is not enabled |
| `applicationId was not found` error | The launcher template no longer declares `applicationId` |
| `<path> not found` warning | That store's SDK is not imported, or the plugin path changed in a newer SDK version |
| Dependency lines appear after the closing brace | The `**DEPS**` marker was removed from `mainTemplate.gradle` |
| Two stores' billing libraries end up in one APK | A store setup method was not called with `false` for the excluded store |
