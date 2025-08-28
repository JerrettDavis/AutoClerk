# Drivers (FlaUI / Windows UI Automation)

AutoClerk ships with a FlaUI-based Windows driver that targets UI Automation (UIA3/UIA2).

Packages
- AutoClerk.Driver.FlaUi (implementation)
- AutoClerk.Abstractions (API surface)

Choose a backend
- UIA3 (default): Best for modern Windows/XAML/packaged apps
- UIA2: Sometimes useful on classic Win32/interop surfaces

Creating a session
```csharp
var driver = new FlaUiDriver();
await using var session = await driver.LaunchAsync("notepad.exe");
```

Attaching to a running process
```csharp
await using var session = await driver.AttachAsync(processId: 1234);
```

Launching packaged (Store) apps by AUMID
```csharp
var options = new UiDriverOptions(
    Backend: UiBackend.UIA3,
    DefaultTimeout: TimeSpan.FromSeconds(10),
    MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Calculator')]")
);
await using var session = await driver.LaunchStoreAppAsync(
    "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options);
```

Main window discovery
- Sessions use a lazy root discovery strategy. Provide UiDriverOptions.MainWindowSelector to help the driver bind to the correct window quickly (especially for packaged apps).

Locators and queries
```csharp
var factory = new FlaUiLocatorFactory();
var editor = factory.Create(session, Selector.KindIs(ControlKind.TextBox));
await editor.FillAsync("Hello");
```

Closing the app
```csharp
await session.CloseAsync(discard: true); // dismisses "Don't Save"/"Discard" prompts when possible
```

Notes
- FlaUI translates ControlKind to UIA ControlType (see Selectors).
- Driver methods honor cancellation tokens and timeouts.

