# Examples

This page shows small, focused examples you can copy‑paste to get started.

Notepad: type and close without saving
```csharp
using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;

var driver = new FlaUiDriver();
await using var session = await driver.LaunchAsync("notepad.exe");

var factory = new FlaUiLocatorFactory();
var editor = factory.Create(session, Selector.KindIs(ControlKind.TextBox));
await editor.FillAsync("Hello from AutoClerk!\n");

await session.CloseAsync(discard: true);
```

Calculator (Store app): wait for results
```csharp
using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;

var driver = new FlaUiDriver();
var options = new UiDriverOptions(
    Backend: UiBackend.UIA3,
    DefaultTimeout: TimeSpan.FromSeconds(10),
    MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Calculator')]")
);
await using var session = await driver.LaunchStoreAppAsync("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options);

var factory = new FlaUiLocatorFactory();
var results = factory.Create(session, Selector.XPath("//*[@AutomationId='CalculatorResults']"));
await Expect.That(results).ToBeVisibleAsync();
```

Attach to a running process
```csharp
var driver = new FlaUiDriver();
await using var session = await driver.AttachAsync(processId: 1234);
```

Use locators directly from the session (driver extension)
```csharp
var status = session.Locator(Selector.NameIs("Status"));
await Expect.That(status).ToHaveTextAsync("Ready");
```

DSL‑driven flow
```csharp
var op = new OperationDefinition("Smoke", new[]
{
    new OperationStep("type",  Selector.Id("UserName").With(kind: ControlKind.TextBox), new(){["text"]="jane"}),
    new OperationStep("type",  Selector.Id("Password").With(kind: ControlKind.TextBox), new(){["text"]="secret"}),
    new OperationStep("click", Selector.Id("Login").With(kind: ControlKind.Button)),
    new OperationStep("expectVisible", new Selector(Name: "Welcome", Kind: ControlKind.Label))
});

var exec = new DefaultOperationExecutor(session);
await exec.ExecuteAsync(op);
```

