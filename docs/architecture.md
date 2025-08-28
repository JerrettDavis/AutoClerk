# Architecture

AutoClerk is a small set of clear abstractions designed to be driver‑agnostic.

Core concepts
- IUiDriver: Creates IAppSession by launching or attaching to a running app.
- IAppSession: The live handle for queries and high‑level operations.
- Selector: Immutable value describing how to locate UI elements.
- ILocator: Playwright‑style handle that re‑resolves the element on every action and auto‑waits.
- IElement: A concrete element instance with best‑effort actions (used internally by sessions).
- Expect: Auto‑waiting assertions over locators.

Flow
1) Create an IUiDriver (e.g., FlaUiDriver on Windows).
2) Launch or attach to an app to get an IAppSession.
3) Use an ILocatorFactory to create locators bound to the session + selector.
4) Act through the locator (ClickAsync, FillAsync, PressAsync, TextContentAsync) with LocatorOptions as needed.
5) Assert with Expect.That(locator).ToBeVisibleAsync()/ToHaveTextAsync(...).
6) Close the session (DisposeAsync/CloseAsync) when finished.

Auto‑wait contract (high level)
- Inputs: Selector + optional LocatorOptions (Timeout, PollInterval, Trial).
- Behavior: Re‑probe until the element exists and is ready for the action, or until timeout/cancellation.
- Errors: TimeoutException (unless Trial), OperationCanceledException, NotSupportedException for unsupported actions.
- Success: The action completes without throwing and returns when applied.

Extensibility
- Drivers: Provide IUiDriver and IAppSession implementations translating Selector to the platform.
- Actions: Extend ILocator or add helpers on top (e.g., select, hover) using session queries and IElement.
- DSL: Plug a custom IOperationExecutor to run structured test flows.

