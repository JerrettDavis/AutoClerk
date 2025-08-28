# Troubleshooting

Common issues and fixes when automating Windows apps with AutoClerk.

UI doesn’t respond or elements aren’t found
- Check backend: try UiBackend.UIA3 (default) first, then UiBackend.UIA2 for classic/interop UIs.
- Prefer AutomationId over Name; names often localize or change.
- Add Kind to disambiguate: Selector.Id("Login").With(kind: ControlKind.Button).
- Increase timeouts when apps are slow to render: new LocatorOptions { Timeout = TimeSpan.FromSeconds(10) }.
- For packaged apps, provide UiDriverOptions.MainWindowSelector as a hint to bind the real window.

Packaged app launched but no window is detected
- Launch by AUMID using LaunchStoreAppAsync instead of exe path.
- Supply a MainWindowSelector like Selector.XPath("//Window[contains(@Name,'Calculator')]").
- Be patient: returning from LaunchStoreAppAsync means the app is launching; await a visible control via Expect.

Notepad/Calculator behave inconsistently
- On Windows 11, these are packaged apps; use AUMID and UIA3. Classic notepad.exe exists on some systems only.
- For Notepad save prompts, session.CloseAsync(discard: true) tries to dismiss “Don’t Save”/“Discard”.

Clicks or typing don’t work
- Ensure control is visible and enabled: await Expect.That(locator).ToBeVisibleAsync().
- Some controls require focus; FillAsync and PressAsync focus before input, but custom flows may need a ClickAsync first.
- Try switching backends (UIA2/UIA3) if a control doesn’t expose expected patterns.

Expectations keep timing out
- Verify the selector resolves at all by using QueryAllAsync to see what’s found.
- Use TextContentAsync to inspect raw text; some controls expose Name like “Display is 5” (Calculator).
- For non-fatal checks, set Trial = true to suppress exceptions and keep the flow moving.

Admin/UAC issues
- Some apps require elevated access for UIA; run your test process as admin if elements are missing.
- Avoid interacting across privilege boundaries (admin app vs non-admin test).

Concurrency flakiness
- Don’t run multiple actions concurrently against the same session or locator; await each step.
- UIA providers can race on focus; keep interactions serial.

Logging and diagnostics
- Log selectors used and the driver/backend.
- Capture screenshots via OS tools (Win+Shift+S) or use external libs alongside AutoClerk if needed.

Still stuck?
- Reduce the selector to a simpler shape (e.g., by Kind only), then refine until it matches uniquely.
- Try FlaUI Inspect or Windows Accessibility Insights to inspect available properties/automation IDs.

