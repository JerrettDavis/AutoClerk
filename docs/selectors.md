# Selectors

Selectors describe how to find elements. They’re immutable and driver‑agnostic; drivers translate them to platform queries.

Shape
```csharp
public sealed record Selector(
    string? AutomationId = null,
    string? Name = null,
    ControlKind Kind = ControlKind.Any,
    string? Path = null
)
```

Fields
- AutomationId: Stable identifier; prefer this for resilience.
- Name: Visible name/title; can vary with locale/dynamic content.
- Kind: Logical control category, see ControlKind (Button, TextBox, Window, etc.).
- Path: Driver‑specific path expression (e.g., FlaUI XPath).

Factory helpers
```csharp
var byId   = Selector.Id("UserName");
var byName = Selector.NameIs("Save");
var byKind = Selector.KindIs(ControlKind.Button);
var byPath = Selector.XPath("//*[@AutomationId='CalculatorResults']");
```

Composing
- Combine fields to narrow: Selector.Id("UserName") with { Kind = ControlKind.TextBox }
- Use extensions to copy/override:
```csharp
var s1 = Selector.Id("Login");
var button = s1.With(kind: ControlKind.Button);
```

ControlKind mapping (FlaUI/UIA)
- Window -> ControlType.Window
- Button -> ControlType.Button
- TextBox -> ControlType.Edit
- ComboBox -> ControlType.ComboBox
- List -> ControlType.List
- ListItem -> ControlType.ListItem
- Tab -> ControlType.Tab
- CheckBox -> ControlType.CheckBox
- RadioButton -> ControlType.RadioButton
- MenuItem -> ControlType.MenuItem
- DataGrid -> ControlType.DataGrid
- Label -> ControlType.Text

Guidance
- Prefer AutomationId when available.
- Add Kind to disambiguate when multiple controls share id or name.
- Use Path sparingly for deep/relative searches when ids aren’t available.

See also
- Inspecting UI and Finding Selectors: inspecting-ui.md
