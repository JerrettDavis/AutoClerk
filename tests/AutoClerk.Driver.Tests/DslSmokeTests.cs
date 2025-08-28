using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;

namespace AutoClerk.Driver.Tests;

[Collection("UI")] 
public class DslSmokeTests
{
    [Fact]
    public async Task Notepad_smoke_via_DSL_executor()
    {
        Skippable.WindowsOnly();

        var driver = new FlaUiDriver();
        var options = new UiDriverOptions(
            Backend: UiBackend.UIA3,
            DefaultTimeout: TimeSpan.FromSeconds(6),
            // hint makes EnsureRoot() instant
            MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Notepad')]")
        );

        await using var app = await driver.LaunchStoreAppAsync("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", options);

        // Try fastest shape first; union as fallback if your FlaUI supports it
        var editor = Selector.XPath("//*[@AutomationId='TextEditor'] | //Document[1] | //Edit[1]");

        var op = new OperationDefinition("NotepadSmoke", [
            new OperationStep("expectVisible", editor),
            new OperationStep("type", editor, new() { ["text"] = "Hello from DSL" }),
            new OperationStep("wait", Selector.KindIs(ControlKind.TextBox), new() { ["ms"] = "100" }),
            new OperationStep("expectVisible", editor)
        ]);

        var exec = new DefaultOperationExecutor(app);
        await exec.ExecuteAsync(op);

        // Hard close – no orphans
        await app.CloseAsync(discard: true, timeout: TimeSpan.FromSeconds(1));
    }
}