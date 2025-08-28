# Inspecting UI and Finding Selectors

Use a UI inspector to discover AutomationId, ControlType, and Name for elements, then translate those into AutoClerk
selectors.

Recommended tools

- Accessibility Insights for Windows (AIWin)
    - Download: https://accessibilityinsights.io/docs/en/windows/overview/
    - How: Launch AIWin → Live Inspect → enable element highlight → hover the target → read AutomationId, ControlType,
      Name in the Properties pane.
- FlaUInspect (from FlaUI)
    - Repo: https://github.com/FlaUI/FlaUInspect
    - How: Pick backend (UIA3 first; try UIA2 if needed) → use the crosshair to select → view properties; right‑click
      tree nodes to copy XPath when helpful.
- Inspect.exe (Windows SDK)
    - Docs: https://learn.microsoft.com/windows/win32/winauto/inspect-objects
    - How: Installed with the Windows SDK (Path like: C:\Program Files (x86)\Windows Kits\10\bin\<Version>
      \x86\inspect.exe). Use the crosshair to inspect the target and read properties.

What to look for

- AutomationId: Prefer this when present; it’s stable and localization‑independent.
- ControlType: Maps to ControlKind (e.g., Edit → TextBox, Button → Button, Text → Label).
- Name: Avoid using as a sole selector when it localizes or changes dynamically.
- XPath: Helpful for deep or relative queries when AutomationId isn’t available.

Translate to AutoClerk selectors

- Calculator results (AIWin/FlaUInspect often shows):
    - AutomationId: "CalculatorResults"
    - ControlType: Text
    - Name: e.g., "Display is 0"

```csharp
// Prefer Id, optionally add Kind to disambiguate
var results = Selector.Id("CalculatorResults").With(kind: ControlKind.Label);

// Or use a path (driver‑specific XPath)
var resultsByPath = Selector.XPath("//*[@AutomationId='CalculatorResults']");
```

- Notepad editor (classic)
    - ControlType: Edit
    - AutomationId: often empty

```csharp
// Constrain by kind only when needed
var editor = new Selector(Kind: ControlKind.TextBox);
```

Driver mapping reference

- ControlType ↔ ControlKind (FlaUI/UIA mapping):
    - Window → Window
    - Button → Button
    - Edit → TextBox
    - ComboBox → ComboBox
    - List → List, ListItem → ListItem
    - Tab → Tab
    - CheckBox → CheckBox
    - RadioButton → RadioButton
    - MenuItem → MenuItem
    - DataGrid → DataGrid
    - Text → Label

Tips for robust selectors

- Prefer AutomationId; combine with Kind for clarity.
- Avoid Name unless it’s stable. If you must, combine Name with Kind.
- Use XPath sparingly for deep structures or when ids are missing.
- If a control doesn’t appear under UIA3, try UIA2 in your inspector and driver.

Optional: find packaged app AUMIDs (launching Store apps)

- PowerShell: Get‑StartApps | ? Name -like "*Calculator*"
- Use the AUMID with LaunchStoreAppAsync("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options)

Further reading

- UI Automation control types
  overview: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-controltypesoverview
- UI Automation properties overview: https://learn.microsoft.com/windows/win32/winauto/uiauto-automation-element-propids
- FlaUI project: https://github.com/FlaUI/FlaUI

