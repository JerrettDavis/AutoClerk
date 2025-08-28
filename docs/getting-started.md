# Getting Started

This guide walks you through installing AutoClerk, launching an app, locating elements, and running simple actions.

Prerequisites
- Windows 10/11 when using the FlaUI driver
- .NET 8 or later

Install packages
- From source: add project references to AutoClerk.Abstractions and AutoClerk.Driver.FlaUi
- From NuGet (when published):
  - AutoClerk.Abstractions
  - AutoClerk.Driver.FlaUi

Quickstart (Windows, Notepad)
```csharp
using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;

// Create the driver and launch Notepad (classic exe)
var driver = new FlaUiDriver();
await using var session = await driver.LaunchAsync("notepad.exe");

// Create a locator factory and target the main edit area by ControlKind and/or attributes.
var factory = new FlaUiLocatorFactory();
var editor = factory.Create(session, new Selector(Kind: ControlKind.TextBox));

await editor.FillAsync("Hello from AutoClerk!\n");
await editor.PressAsync("Ctrl+S");

// Close without saving
await session.CloseAsync(discard: true);
```

Launching packaged (Store) apps
```csharp
using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;

var driver = new FlaUiDriver();
var options = new UiDriverOptions(
    Backend: UiBackend.UIA3,
    DefaultTimeout: TimeSpan.FromSeconds(10),
    MainWindowSelector: new Selector(Kind: ControlKind.Window, Name: "Calculator")
);

await using var session = await driver.LaunchStoreAppAsync(
    "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options);

var factory = new FlaUiLocatorFactory();
var results = factory.Create(session, new Selector(AutomationId: "CalculatorResults"));
await Expect.That(results).ToBeVisibleAsync();
```

Project structure hints
- IUiDriver creates IAppSession (launch or attach)
- IAppSession performs queries and convenience actions
- ILocator provides Playwright-style auto-waiting operations
- Expect offers assertion utilities with auto-wait

Next steps
- Read Architecture for concepts, Selectors for targeting, and Drivers/FlaUI for platform specifics.

