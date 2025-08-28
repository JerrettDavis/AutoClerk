using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;
using Shouldly;

namespace AutoClerk.Driver.Tests;

public class NotepadTests
{
    [Fact]
    public async Task Notepad_can_fill_and_read_back_text()
    {
        Skippable.WindowsOnly();

        var driver = new FlaUiDriver();
        var factory = new FlaUiLocatorFactory();

        var options = new UiDriverOptions(
            Backend: UiBackend.UIA3,
            DefaultTimeout: TimeSpan.FromSeconds(10),
            // robust window hint for packaged Notepad on Win11
            MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Notepad')]")
        );

        await using var app = await driver.LaunchStoreAppAsync(
            "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", options);

        // Wait until the editor exists (probing several shapes)
        var editor = await NotepadSelectors.FindEditorAsync(app, factory, TimeSpan.FromSeconds(8));

        var text = $"Hello AutoClerk! {DateTime.Now:HHmmss}";
        await editor.FillAsync(text);

        // Read back (GetTextAsync now: TextPattern → ValuePattern → Legacy → Name)
        var readBack = await editor.TextContentAsync();
        readBack.ShouldBe(text);
    }
}