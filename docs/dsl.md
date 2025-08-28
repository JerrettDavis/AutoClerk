# DSL and Operation Executor

AutoClerk includes a minimal, data‑driven operation executor intended for simple scripted flows defined in JSON/YAML.

Key types
- OperationStep: { Action, Target: Selector, Args?: Dictionary<string,string> }
- OperationDefinition: { Name, Steps: IReadOnlyList<OperationStep> }
- DefaultOperationExecutor: Interprets the definition and runs steps sequentially against an IAppSession

Built‑in actions
- click: Clicks the target element.
- type: Types text into the target; requires Args.text.
- expectVisible: Waits for the target to become visible; throws on timeout.
- wait: Sleeps for Args.ms milliseconds (default 250).

Example
```csharp
var op = new OperationDefinition(
    "Login",
    new[]
    {
        new OperationStep("type",  Selector.Id("UserName").With(kind: ControlKind.TextBox), new() { ["text"] = "jane" }),
        new OperationStep("type",  Selector.Id("Password").With(kind: ControlKind.TextBox), new() { ["text"] = "secret" }),
        new OperationStep("click", Selector.Id("Login").With(kind: ControlKind.Button)),
        new OperationStep("expectVisible", new Selector(Name: "Welcome", Kind: ControlKind.Label)),
        new OperationStep("wait", Selector.KindIs(ControlKind.Window), new() { ["ms"] = "150" })
    });

var exec = new DefaultOperationExecutor(session);
await exec.ExecuteAsync(op, ct);
```

Extending actions
- Add new verbs (e.g., press, select, hover, close) by extending DefaultOperationExecutor’s switch and mapping to IAppSession/ILocator operations.
- Validate inputs and throw ArgumentException for missing/invalid args.

When to use
- Simple smoke tests and demo scripts driven by external artifacts
- Not a replacement for a full test framework; compose higher‑level steps as needed

