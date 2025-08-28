# Expectations

Expectations provide Playwright‑style, auto‑waiting assertions on top of locators.

Entry point
```csharp
var banner = factory.Create(session, Selector.Id("Welcome"));
await Expect.That(banner).ToBeVisibleAsync();
```

APIs
- ToBeVisibleAsync(options?, ct): Waits until the element becomes visible. Throws TimeoutException on failure by default.
- ToBeEnabledAsync(options?, ct): Waits until the element becomes enabled.
- ToHaveTextAsync(expected, options?, ct): Waits until the element’s visible text/value equals expected (case‑sensitive, Ordinal).

Options
- Timeout: Overrides the wait budget. Defaults to session DefaultTimeout when not specified.
- PollInterval: Probe cadence (typically ~50 ms).
- Trial: If true, expectations complete silently on timeout (don’t throw). Useful for non‑fatal checks.

Examples
```csharp
// Faster probe (single tick)
await Expect.That(status).ToBeVisibleAsync(new LocatorOptions { Timeout = TimeSpan.Zero });

// Longer wait with tight polling
var wait = new LocatorOptions { Timeout = TimeSpan.FromSeconds(10), PollInterval = TimeSpan.FromMilliseconds(50) };
await Expect.That(dialog).ToBeVisibleAsync(wait);

// Non‑fatal check
var trial = new LocatorOptions { Timeout = TimeSpan.FromSeconds(2), Trial = true };
await Expect.That(toast).ToHaveTextAsync("Saved", trial);
```

Notes
- Expectations re‑resolve the locator each probe and do not cache element handles.
- Use ToHaveTextAsync for exact matches; add your own helper for partial/regex matches if needed.

