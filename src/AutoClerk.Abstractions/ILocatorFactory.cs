namespace AutoClerk.Abstractions;

/// <summary>
/// Factory for creating driver-backed <see cref="ILocator"/> instances bound to an <see cref="IAppSession"/> and a <see cref="Selector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are typically lightweight and stateless: they construct a locator that captures the
/// provided <paramref name="session"/> and <paramref name="selector"/> and defers all element resolution
/// to action time (i.e., no UI handle is cached at creation).
/// </para>
/// <para>
/// <b>Thread-safety:</b> A single factory instance should be safe to reuse across threads and sessions.
/// The returned <see cref="ILocator"/> objects are immutable, but callers should avoid running multiple
/// actions concurrently on the same locator.
/// </para>
/// <para>
/// <b>Typical usage:</b>
/// </para>
/// <code>
/// // Resolve through the factory directly:
/// var factory = new FlaUiLocatorFactory(); // driver-specific implementation
/// var login   = factory.Create(session, Selector.Id("LoginButton"));
/// await login.ClickAsync();
///
/// // Or use the convenience extension (if you added one):
/// var login2 = session.Locator(factory, Selector.Id("LoginButton"));
/// await Expect.That(login2).ToBeVisibleAsync();
/// </code>
/// </remarks>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="Selector"/>
public interface ILocatorFactory
{
    /// <summary>
    /// Creates a new <see cref="ILocator"/> bound to the specified <paramref name="session"/> and <paramref name="selector"/>.
    /// </summary>
    /// <param name="session">The live application session the locator should operate against.</param>
    /// <param name="selector">The selector describing the target element(s) to resolve at action time.</param>
    /// <returns>
    /// A Playwright-style locator that will (re)resolve elements using <paramref name="selector"/> on each action,
    /// honoring the session’s defaults (e.g., timeouts) unless overridden per call.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="session"/> or <paramref name="selector"/> is <c>null</c>.
    /// </exception>
    ILocator Create(IAppSession session, Selector selector);
}