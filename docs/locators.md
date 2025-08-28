# Locators

Locators provide Playwright-style ergonomics: each action re-resolves the element and auto-waits until it’s ready.

Key types
- ILocator: The action surface (ClickAsync, FillAsync, PressAsync, TextContentAsync, IsVisibleAsync, IsEnabledAsync)
- ILocatorFactory: Creates locators bound to an IAppSession + Selector
- LocatorOptions: Per-call overrides (Timeout, PollInterval, Trial)

Create a locator
```csharp
var factory = new FlaUiLocatorFactory();
var userName = factory.Create(session, Selector.Id("UserName").With(kind: ControlKind.TextBox));
```

Actions
```csharp
await userName.FillAsync("jane.doe");
await userName.PressAsync("ENTER");
await Expect.That(userName).ToBeVisibleAsync();
var value = await userName.TextContentAsync();
```

Timeouts and polling
```csharp
var slow = new LocatorOptions { Timeout = TimeSpan.FromSeconds(8), PollInterval = TimeSpan.FromMilliseconds(50) };
await login.ClickAsync(slow);
```

Fast probes
- IsVisibleAsync and IsEnabledAsync are quick, non-throwing checks bounded by a small probe window.
```csharp
if (await toast.IsVisibleAsync()) { /* ... */ }
```

When to use locators vs sessions
- Locators: everyday interactions with built-in waits and tidy call sites.
- Sessions: raw queries (QueryAsync/QueryAllAsync), custom waits, and convenience actions where you don’t need re-resolution.

