# AutoClerk

Playwright-style UI automation for classic Windows apps — powered by FlaUI, with a clean, driver-agnostic abstraction
layer and a tiny DSL you can feed from YAML/JSON/XML.

> **Why?**
> You’ve got a .NET Framework 4.8 WinForms app that usually needs a human at a terminal. AutoClerk gives you a
> consistent, modern, **Playwright-like** API (locators, auto-waits, expectations) on top of Windows UI Automation so you
> can author, run, and scale tests without bespoke scripts.

---

## Packages & Layout

```
src/
  AutoClerk.Abstractions/          // Core contracts (IUiDriver, IAppSession, ILocator, expectations, selectors)
  AutoClerk.Driver.FlaUi/          // UIA2/UIA3 driver using FlaUI + robust Notepad/Store-app handling
tests/
  AutoClerk.Driver.Tests/          // Smoke tests (Calculator, Notepad, DSL executor)
docs/
  docfx.json                       // DocFX config (API + conceptual docs)  ← optional if you haven’t added yet
```

---

## Key Concepts

* **Abstractions**:
  `IUiDriver`, `IAppSession`, `ILocator`, `ILocatorFactory`, `Expect`, `Selector`, `LocatorOptions`, `ControlKind`.
* **FlaUI driver**:
  `FlaUiDriver`, `DriverStoreAppExtensions` (AUMID launch), `FlaUiAppSession`, `FlaUiElement`, `LocatorShim`.
* **DSL executor**:
  `OperationDefinition` + `OperationStep` + `DefaultOperationExecutor` (verbs: `click`, `type`, `expectVisible`,`wait`).

Everything is documented with production-ready XML comments so you get rich DocFX output and first-class IntelliSense.

---

## Requirements

* Windows 10/11 (UI Automation)
* .NET SDK 8.0+ (build/tests)
* Test runner: xUnit in `AutoClerk.Driver.Tests`
* **FlaUI.UIA3** (default) or **FlaUI.UIA2** (legacy surfaces)

> Tip: Windows 11 **Notepad** and **Calculator** are packaged apps. Launch them by **AUMID** for reliability.

---

## Quick Start

### 1) Launch a packaged app (Calculator) and assert a result

```csharp
await using var session = await new FlaUiDriver()
    .LaunchStoreAppAsync("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
        new UiDriverOptions(
            Backend: UiBackend.UIA3,
            DefaultTimeout: TimeSpan.FromSeconds(8),
            MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Calculator')]")));

var loc = new FlaUiLocatorFactory();

// Click 2 + 3 = and read the result
var two   = session.Locator(loc, Selector.NameIs("Two").With(kind: ControlKind.Button));
var plus  = session.Locator(loc, Selector.NameIs("Plus").With(kind: ControlKind.Button));
var three = session.Locator(loc, Selector.NameIs("Three").With(kind: ControlKind.Button));
var eq    = session.Locator(loc, Selector.NameIs("Equals").With(kind: ControlKind.Button));

await two.ClickAsync();
await plus.ClickAsync();
await three.ClickAsync();
await eq.ClickAsync();

var result = session.Locator(loc, Selector.XPath("//*[@AutomationId='CalculatorResults']"));
await Expect.That(result).ToBeVisibleAsync();
Console.WriteLine(await result.TextContentAsync());  // e.g., "Display is 5"
```

### 2) Launch a classic app (Notepad) and use the DSL executor

```csharp
await using var session = await new FlaUiDriver().LaunchAsync("notepad.exe",
    options: new UiDriverOptions(
        Backend: UiBackend.UIA3,
        DefaultTimeout: TimeSpan.FromSeconds(8),
        MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Notepad')]")));

var op = new OperationDefinition(
    "NotepadSmoke",
    [
        new OperationStep("type",  Selector.XPath("//Edit[1]"), new() { ["text"] = "Hello from DSL" }),
        new OperationStep("wait",  Selector.KindIs(ControlKind.TextBox), new() { ["ms"] = "250" }),
        new OperationStep("expectVisible", Selector.XPath("//Edit[1]")),
    ]);

await new DefaultOperationExecutor(session).ExecuteAsync(op);
```

> **Cleanup:** Sessions are `IAsyncDisposable`. `await using` will call `CloseAsync` (graceful WindowPattern close →
> dismiss “Don’t Save” → WM\_CLOSE → only kill `notepad.exe` as a last resort).

---

## DSL Shape (spec/gherkin-ish)

Operations are data models; you can build them in memory or read from YAML/JSON/XML (see **Roadmap** below).

```yaml
# notepad_smoke.yml
name: NotepadSmoke
steps:
  - action: type
    target: { path: "//Edit[1]" }
    args: { text: "Hello from DSL" }

  - action: wait
    target: { kind: TextBox }
    args: { ms: "250" }

  - action: expectVisible
    target: { path: "//Edit[1]" }
```

```json
{
  "name": "LoginSmoke",
  "steps": [
    {
      "action": "type",
      "target": {
        "automationId": "UserName",
        "kind": "TextBox"
      },
      "args": {
        "text": "jane"
      }
    },
    {
      "action": "type",
      "target": {
        "automationId": "Password",
        "kind": "TextBox"
      },
      "args": {
        "text": "secret"
      }
    },
    {
      "action": "click",
      "target": {
        "automationId": "Login",
        "kind": "Button"
      }
    },
    {
      "action": "expectVisible",
      "target": {
        "name": "Welcome",
        "kind": "Label"
      }
    }
  ]
}
```

Model mapping (already in code):

* `OperationDefinition(string Name, IEnumerable<OperationStep> Steps)`
* `OperationStep(string Action, Selector Target, Dictionary<string,string>? Args)`
* `Selector(AutomationId?, Name?, Kind?, Path?)` (+ fluent `With()` extensions)

Verbs implemented: `click`, `type` (`Args.text`), `expectVisible`, `wait` (`Args.ms`).

---

## Running Tests

```bash
dotnet build
dotnet test tests/AutoClerk.Driver.Tests
```

Notes:

* **Windows-only** tests are guarded with `Skippable.WindowsOnly()` where relevant.
* **Packaged apps**: The test suite uses **AUMID** launch for Calculator. Notepad can be classic or packaged on Win11;
  the driver handles both.
* If a test ever leaves an orphaned Notepad, the session’s `DisposeAsync` fallback will try to close it. If you’re
  running tests in parallel, consider test-level isolation (see **Roadmap → Stability & Parallelism**).

---

## Documentation (DocFX)

The codebase is fully XML-documented. If you add a `docs/docfx.json`, you can publish a site:

```bash
dotnet tool update -g docfx
docfx docs/docfx.json
# Output typically in docs/_site
```

Recommended sections: **Getting Started**, **Selectors**, **Expectations**, **DSL Reference**, **Driver Internals**.

---

## Design Goals

* **Playwright-like ergonomics** on Windows UIA: locators, auto-waits, expectations.
* **Driver-agnostic core**: abstractions live in `AutoClerk.Abstractions`.
* **Composable DSL**: execute “features” without hard-coding flows.
* **Robustness**: UITree changes & packaged app quirks handled by lazy root discovery and safe property reads.
* **First-class docs**: dense XML comments suitable for DocFX.

---

## Roadmap / Next Steps

### 1) DSL I/O (YAML/JSON/XML)

* **Readers**: Implement loaders that deserialize feature files to `OperationDefinition`.

    * `AutoClerk.TestRunner` project: `YamlOperationReader`, `JsonOperationReader`, `XmlOperationReader`.
    * Schema validation (e.g., JSON Schema / XSD).
    * Friendly diagnostics (file+line on malformed steps).
* **Extensible verbs**: Add `press`, `select`, `hover`, `expectEnabled`, `expectText`, `expectContainsText`, `close`,
  `screenshot`.

### 2) ReqNRoll (Cucumber/Gherkin) Integration

* Add `AutoClerk.ReqnRoll` project with step bindings:

    * `Given I launch "<app>"`, `When I click "<selector>"`, `Then I expect "<selector>" is visible`, etc.
* Parse selector mini-DSL in step parameters (`id=Login; kind=Button`, `xpath=//Edit[1]`).
* Tag-based environment selection (UIA2 vs UIA3, timeouts).
* Parallel scenarios (feature-level session isolation).

### 3) Service Runner (Headless Orchestrator)

* Dedicated Windows service/worker that accepts operation payloads via REST/queue:

    * `POST /run` → enqueue feature file → produce result (pass/fail/logs/artifacts).
    * Multi-tenant run directories, process sandboxing.
    * Concurrency guards (one UI session per desktop/VM).
* Observability: structured logs, per-step timings, screenshots on failure, optional video via Windows Graphics Capture.

### 4) Stability & Parallelism

* **Session isolation**: Named virtual desktops or isolated Windows sessions if you run multiple suites.
* **Retry policy**: Optional exponential backoff in `Retry.SpinWaitAsync`.
* **Better close routines**: Expand “Don’t Save” synonyms/localization; detect modal dialogs generically.
* **Selector hardening**: Support class names, descendant scoping, nth-child helpers, and fuzzy name matching.

### 5) Developer Experience

* **CLI** (`autoclerk run --file feature.yml --timeout 30s`).
* **Recorder** (optional): capture clicks → emit selectors into YAML.
* **Typed keys**: Add key chords & text input speed/throttling.
* **Localization**: Prefer AutomationIds over Names; provide resource maps per language.
* **Test fixtures**: xUnit/MSTest/NUnit helpers for session lifecycle, flake-resistant patterns.

### 6) CI/CD

* GitHub Actions (Windows runners):

    * Build, test, publish artifacts (logs/screenshots).
    * Optional nightly run against a test VM.
* NuGet packaging:

    * `AutoClerk.Abstractions`, `AutoClerk.Driver.FlaUi`, `AutoClerk.Dsl` (models), `AutoClerk.TestRunner`.

### 7) Security & Governance

* App whitelisting for launches (avoid arbitrary process exec).
* Sandboxing (least privileges) and telemetry opt-in.

---

## Troubleshooting

* **Packaged apps not detected**
  Launch by **AUMID** (`DriverStoreAppExtensions.LaunchStoreAppAsync`) and provide a `MainWindowSelector` hint:
  `Selector.XPath("//Window[contains(@Name,'Notepad')]")`.

* **Calculator text retrieval fails**
  We already fall back across TextPattern → ValuePattern → LegacyIAccessible → TextBox → Name. If your locale changes
  button names (“Two”, “Equals”), switch to **AutomationId** selectors.

* **Accessibility assembly missing**
  Ensure the `Accessibility` assembly is available (Windows ships it; .NET sometimes needs a binding redirect in legacy
  scenarios). FlaUI’s UIA3 uses the LegacyIAccessible pattern internally when exposed.

* **Notepad doesn’t close**
  Session `DisposeAsync` runs a close sequence (WindowPattern → title-bar Close → dismiss “Don’t Save” → WM\_CLOSE → *
  *only kill classic notepad.exe**). If you see orphans, confirm you’re `await using` the session.

---

## Contributing

1. Create a feature branch.
2. Keep public surfaces fully XML-documented.
3. Add/expand tests (prefer stable AutomationIds).
4. Run `dotnet format` and `dotnet test`.
5. Open a PR with a clear description and before/after notes.

---

## License

MIT 

---

## Acknowledgments

* **FlaUI** — a fantastic .NET UIA wrapper powering the driver layer.
* Playwright — inspiration for locator ergonomics and expectations.
