using System.Text.RegularExpressions;
using AutoClerk.Abstractions;
using AutoClerk.Driver.FlaUi;
using Shouldly;

namespace AutoClerk.Driver.Tests;

public class CalculatorTests
{
    [Fact]
    public async Task Calculator_addition_2_plus_3_equals_5()
    {
        Skippable.WindowsOnly();

        var driver = new FlaUiDriver();
        var factory = new FlaUiLocatorFactory();

        var options = new UiDriverOptions(
            Backend: UiBackend.UIA3,
            DefaultTimeout: TimeSpan.FromSeconds(10),
            MainWindowSelector: new Selector(Name: "Calculator", Kind: ControlKind.Window) // hint for the real window
        );

        // Launch the packaged Calc by AUMID (Win11 reliable way)
        await using var app = await driver.LaunchStoreAppAsync(
            "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options);

        // Wait until the result control exists/visible (Calc is ready)
        var result = app.Locator(factory, Selector.XPath("//*[@AutomationId='CalculatorResults']"));
        await Expect.That(result).ToBeVisibleAsync(new LocatorOptions { Timeout = TimeSpan.FromSeconds(10) });

        // Use AutomationIds for buttons (stable on English builds)
        var btnTwo = app.Locator(factory, Selector.Id("num2Button").With(kind: ControlKind.Button));
        var btnPlus = app.Locator(factory, Selector.Id("plusButton").With(kind: ControlKind.Button));
        var btnThree = app.Locator(factory, Selector.Id("num3Button").With(kind: ControlKind.Button));
        var btnEq = app.Locator(factory, Selector.Id("equalButton").With(kind: ControlKind.Button));

        await btnTwo.ClickAsync();
        await btnPlus.ClickAsync();
        await btnThree.ClickAsync();
        await btnEq.ClickAsync();

        var text = await result.TextContentAsync();           // e.g., "Display is 5"
        var number = ExtractLastNumber(text ?? string.Empty); // parse last integer
        number.ShouldBe(5);
    }

    private static int ExtractLastNumber(string s)
    {
        var m = Regex.Matches(s, @"\d+").LastOrDefault();
        return m is null ? 0 : int.Parse(m.Value);
    }
}