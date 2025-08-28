using AutoClerk.Abstractions;

namespace AutoClerk.Driver.Tests;

internal static class NotepadSelectors
{
    public static async Task<ILocator> FindEditorAsync(IAppSession app, ILocatorFactory factory, TimeSpan timeout)
    {
        var candidates = new[]
        {
            Selector.XPath("//*[@AutomationId='TextEditor']"),
            Selector.XPath("//Edit[1]"),
            Selector.XPath("//*[contains(@ClassName,'RichEdit')]"),
            Selector.XPath("//Document[1]") // sometimes XAML exposes Document
        };

        var deadline = DateTime.UtcNow + timeout;
        var opts = new LocatorOptions { Timeout = TimeSpan.FromMilliseconds(100) };

        while (DateTime.UtcNow < deadline)
        {
            foreach (var sel in candidates)
            {
                try
                {
                    var loc = app.Locator(factory, sel);
                    if (await loc.IsVisibleAsync(opts)) return loc;
                }
                catch
                {
                    /* ignore and keep probing */
                }
            }

            await Task.Delay(150);
        }

        throw new TimeoutException("Could not find Notepad editor via known selectors.");
    }
}