using AutoClerk.Abstractions;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// FlaUI-backed implementation of <see cref="ILocatorFactory"/> that produces driver-bound locators.
/// </summary>
/// <remarks>
/// <para>
/// The returned <see cref="ILocator"/> is immutable and Playwright-style: it does not cache a UI handle.
/// Each action re-resolves the element against the bound <see cref="IAppSession"/> using the provided
/// <see cref="Selector"/> and applies the driver’s auto-waiting semantics.
/// </para>
/// <para>
/// This factory is lightweight and stateless and can be reused across sessions and threads.
/// </para>
/// </remarks>
/// <seealso cref="ILocatorFactory"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="Selector"/>
public sealed class FlaUiLocatorFactory : ILocatorFactory
{
    /// <summary>
    /// Creates a new locator bound to the specified <paramref name="session"/> and <paramref name="selector"/>.
    /// </summary>
    /// <param name="session">The live application session the locator will operate against.</param>
    /// <param name="selector">The selector describing the target element(s) to resolve at action time.</param>
    /// <returns>
    /// An <see cref="ILocator"/> that (re)resolves elements using <paramref name="selector"/> on each action,
    /// honoring session defaults unless overridden per call.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="session"/> or <paramref name="selector"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// var factory = new FlaUiLocatorFactory();
    /// var userName = factory.Create(session, Selector.Id("UserName").With(kind: ControlKind.TextBox));
    /// await userName.FillAsync("jane.doe");
    /// </code>
    /// </example>
    public ILocator Create(
        IAppSession session,
        Selector selector)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selector);

        return new LocatorShim(session, selector);
    }
}