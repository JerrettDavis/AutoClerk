# AutoClerk

A lightweight, driver-agnostic UI automation toolkit for .NET with a Playwright-inspired developer experience.

- Simple, composable abstractions: IUiDriver, IAppSession, ILocator, IElement
- Fluent, auto-waiting expectations: Expect.That(locator).ToBeVisibleAsync()
- Driver-agnostic selectors with ControlKind to constrain queries
- Minimal DSL support for JSON/YAML-driven flows via DefaultOperationExecutor
- FlaUI-based Windows UIAutomation (UIA3/UIA2) driver included

AutoClerk favors predictable, explicit APIs and avoids global state. It’s a good fit for smoke tests, demo scripts, and pragmatic desktop UI automation where you want to script flows without a large framework.


