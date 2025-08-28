namespace AutoClerk.Abstractions;

/// <summary>
/// Convenience extensions for creating Playwright-style locators from an <see cref="IAppSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// This helper forwards to the provided <see cref="ILocatorFactory"/> and returns an immutable
/// <see cref="ILocator"/> bound to the given session and selector. The locator re-resolves the target
/// element on each action and applies driver-level auto-waiting semantics.
/// </para>
/// <para>
/// Using this extension keeps call sites concise and avoids taking a direct dependency on the concrete
/// driver implementation at usage points.
/// </para>
/// </remarks>
/// <seealso cref="ILocatorFactory"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="Selector"/>
public static class LocatorFactoryExtensions
{
    /// <summary>
    /// Creates a locator bound to <paramref name="session"/> and <paramref name="selector"/> using <paramref name="factory"/>.
    /// </summary>
    /// <param name="session">The live application session that the locator should operate against.</param>
    /// <param name="factory">The factory responsible for constructing driver-backed locators.</param>
    /// <param name="selector">The selector describing the target element(s) to resolve at action time.</param>
    /// <returns>
    /// An immutable <see cref="ILocator"/> that (re)resolves elements using <paramref name="selector"/> on each action,
    /// honoring session defaults unless overridden per call.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="session"/>, <paramref name="factory"/>, or <paramref name="selector"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// var factory = new FlaUiLocatorFactory(); // driver-specific implementation
    /// var login   = session.Locator(factory, Selector.Id("LoginButton"));
    /// await login.ClickAsync();
    /// </code>
    /// </example>
    public static ILocator Locator(
        this IAppSession session,
        ILocatorFactory factory,
        Selector selector)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(selector);

        return factory.Create(session, selector);
    }
}