# AutoClerk

A lightweight, driver‑agnostic UI automation toolkit for .NET with a Playwright‑inspired developer experience.

## Quick links
- Documentation: docs/
- API Reference: api/
- Getting Started: docs/getting-started.md
- Inspecting UI: docs/inspecting-ui.md

## Local docs preview
```powershell
# Install/Update DocFX globally (once)
dotnet tool update -g docfx

# From repo root: serve and auto‑rebuild on changes
docfx --serve
```

## What you get
- Simple, composable abstractions (IUiDriver, IAppSession, ILocator, IElement)
- Fluent, auto‑waiting expectations (Expect.That(locator).ToBeVisibleAsync())
- Driver‑agnostic Selectors with ControlKind
- Windows UI Automation driver via FlaUI (UIA3/UIA2)
