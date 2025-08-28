using AutoClerk.Abstractions;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Driver-side convenience extensions for creating Playwright-style <see cref="ILocator"/>s
/// directly from an <see cref="IAppSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// This helper constructs a driver-backed locator (internally implemented by <c>LocatorShim</c>)
/// without requiring a separate <see cref="ILocatorFactory"/>. The returned locator is immutable
/// and does not cache a UI handle; each action re-resolves the element using the bound
/// <see cref="Selector"/> and honors per-call <see cref="LocatorOptions"/> (falling back to
/// <see cref="IAppSession.DefaultTimeout"/> and driver defaults).
/// </para>
/// <para>
/// If you prefer to keep call sites driver-agnostic, use the abstractions-level
/// <c>LocatorFactoryExtensions.Locator(...)</c> with an <see cref="ILocatorFactory"/> implementation
/// instead. This extension is a pragmatic shortcut within the FlaUI driver package.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await using var session = await driver.LaunchStoreAppAsync("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App");
/// var results = session.Locator(Selector.XPath("//*[@AutomationId='CalculatorResults']"));
/// await Expect.That(results).ToBeVisibleAsync();
/// </code>
/// </example>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="Selector"/>
/// <seealso cref="LocatorOptions"/>
public static class SessionLocatorExtensions
{
    /// <summary>
    /// Creates a locator bound to the given <paramref name="session"/> and <paramref name="selector"/>.
    /// </summary>
    /// <param name="session">The live application session the locator should operate against.</param>
    /// <param name="selector">The selector describing the target element(s) to resolve at action time.</param>
    /// <returns>
    /// An <see cref="ILocator"/> that (re)resolves elements using <paramref name="selector"/> on each action,
    /// honoring session defaults unless overridden per call.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="session"/> or <paramref name="selector"/> is <c>null</c>.
    /// </exception>
    public static ILocator Locator(this IAppSession session, Selector selector)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selector);

        // LocatorShim is internal to the driver and implements ILocator.
        return new LocatorShim(session, selector);
    }
}