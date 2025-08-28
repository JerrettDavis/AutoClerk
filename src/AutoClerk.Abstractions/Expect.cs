namespace AutoClerk.Abstractions;

/// <summary>
/// Entry point for Playwright-style, auto-waiting expectations against UI elements.
/// </summary>
/// <remarks>
/// <para>
/// Expectations repeatedly probe a condition (e.g., visibility, text) until it succeeds or the
/// effective timeout elapses. By default, a failure raises a <see cref="TimeoutException"/>.
/// If <see cref="LocatorOptions.Trial"/> is set to <c>true</c>, the expectation completes
/// silently on timeout (no exception) — useful for non-fatal checks in larger flows.
/// </para>
/// <para>
/// Expectations do not cache UI handles: each probe reuses the underlying locator’s resolution,
/// so they remain robust as the UI changes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var banner = app.Locator(Selector.Id("WelcomeBanner"));
/// await Expect.That(banner).ToBeVisibleAsync(new LocatorOptions { Timeout = TimeSpan.FromSeconds(5) });
/// await Expect.That(banner).ToHaveTextAsync("Welcome, Jane");
/// </code>
/// </example>
/// <seealso cref="ExpectLocatorAssertions"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="LocatorOptions"/>
public static class Expect
{
    /// <summary>
    /// Creates an assertion helper bound to the specified <paramref name="locator"/>.
    /// </summary>
    /// <param name="locator">The locator to assert against.</param>
    /// <returns>An assertion helper exposing expectation methods for the locator.</returns>
    public static ExpectLocatorAssertions That(ILocator locator) => new(locator);
}

/// <summary>
/// Assertion helpers for a single locator with built-in auto-waiting.
/// </summary>
/// <remarks>
/// <para>
/// Each expectation polls at the cadence specified by <see cref="LocatorOptions.PollInterval"/>
/// (or a sensible default) until the condition is met or the timeout elapses.
/// </para>
/// <para>
/// <b>Timeout semantics</b>:
/// If <see cref="LocatorOptions.Timeout"/> is <c>null</c>, the session’s
/// <see cref="IAppSession.DefaultTimeout"/> is used. A <see cref="TimeoutException"/> is thrown on failure
/// unless <see cref="LocatorOptions.Trial"/> is <c>true</c>, in which case the method completes without throwing.
/// </para>
/// <para><b>Thread-safety:</b> Expectation instances are lightweight; do not run multiple expectations
/// concurrently against the same locator.</para>
/// </remarks>
/// <seealso cref="Expect"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="LocatorOptions"/>
public sealed class ExpectLocatorAssertions
{
    private readonly ILocator _locator;

    /// <summary>
    /// Creates a new instance bound to <paramref name="locator"/>.
    /// </summary>
    /// <param name="locator">The locator under test.</param>
    internal ExpectLocatorAssertions(ILocator locator) => _locator = locator;

    /// <summary>
    /// Expects the element to be visible.
    /// </summary>
    /// <param name="options">
    /// Per-call options (timeout, polling, and trial mode). If <c>null</c>, defaults are used.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the expectation passes or fails.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element does not become visible within the effective timeout and
    /// <see cref="LocatorOptions.Trial"/> is <c>false</c> (default).
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <example>
    /// <code>
    /// await Expect.That(app.Locator(Selector.Id("Status"))).ToBeVisibleAsync(
    ///     new LocatorOptions { Timeout = TimeSpan.FromSeconds(3) });
    /// </code>
    /// </example>
    public Task ToBeVisibleAsync(
        LocatorOptions? options = null,
        CancellationToken ct = default) =>
        ExpectCore(
            async () => await _locator.IsVisibleAsync(options, ct),
            "to be visible",
            options,
            ct);

    /// <summary>
    /// Expects the element to be enabled (interactable).
    /// </summary>
    /// <param name="options">Per-call options (timeout/polling/trial).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the expectation passes or fails.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element does not become enabled within the effective timeout and
    /// <see cref="LocatorOptions.Trial"/> is <c>false</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public Task ToBeEnabledAsync(
        LocatorOptions? options = null,
        CancellationToken ct = default)
        => ExpectCore(
            async () => await _locator.IsEnabledAsync(options, ct),
            "to be enabled",
            options,
            ct);

    /// <summary>
    /// Expects the element to have the specified text (exact, case-sensitive match).
    /// </summary>
    /// <param name="expected">The exact text expected.</param>
    /// <param name="options">Per-call options (timeout/polling/trial).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the expectation passes or fails.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element text never equals <paramref name="expected"/> within the effective timeout and
    /// <see cref="LocatorOptions.Trial"/> is <c>false</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Matching uses <see cref="StringComparison.Ordinal"/> (case-sensitive, culture-invariant).
    /// For partial matches, consider a separate helper such as <c>ToContainTextAsync</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// await Expect.That(app.Locator(Selector.Id("Banner"))).ToHaveTextAsync("Welcome, Jane");
    /// </code>
    /// </example>
    public Task ToHaveTextAsync(
        string expected,
        LocatorOptions? options = null,
        CancellationToken ct = default)
        => ExpectCore(async () =>
            {
                var t = await _locator.TextContentAsync(options, ct) ?? string.Empty;
                return string.Equals(t, expected, StringComparison.Ordinal);
            }, $"to have text \"{expected}\"", options, ct);

    /// <summary>
    /// Core expectation loop: probes until success or timeout, honoring trial mode.
    /// </summary>
    /// <param name="probe">Async predicate that evaluates the expectation.</param>
    /// <param name="message">Human-readable expectation description for error messages.</param>
    /// <param name="options">Timeout/polling/trial options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the expectation succeeds or (optionally) times out.</returns>
    /// <exception cref="TimeoutException">Thrown when the expectation did not pass in time and trial mode is off.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    private static async Task ExpectCore(
        Func<Task<bool>> probe,
        string message,
        LocatorOptions? options,
        CancellationToken ct)
    {
        var timeout = options?.Timeout ?? TimeSpan.FromSeconds(5);
        var poll = options?.PollInterval ?? TimeSpan.FromMilliseconds(50);
        var deadline = DateTime.UtcNow + timeout;

        Exception? lastEx = null;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (await probe().ConfigureAwait(false)) return;
            }
            catch (Exception ex)
            {
                // Keep the most recent exception for context; expectation may still pass on a subsequent probe.
                lastEx = ex;
            }

            await Task.Delay(poll, ct).ConfigureAwait(false);
        }

        if (options?.Trial == true) return;

        throw new TimeoutException(
            $"Expectation timed out after {timeout.TotalMilliseconds:0} ms: expected {message}."
            + (lastEx is null ? string.Empty : $" Last error: {lastEx.GetType().Name}: {lastEx.Message}"));
    }
}