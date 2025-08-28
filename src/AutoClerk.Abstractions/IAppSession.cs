namespace AutoClerk.Abstractions;

/// <summary>
/// Represents a live automation session attached to a single application instance.
/// </summary>
/// <remarks>
/// <para>
/// A session is the root for issuing queries and high-level actions against the target UI. It maintains
/// driver-level defaults (e.g., timeouts) and provides convenience operations (<see cref="ClickAsync"/>,
/// <see cref="TypeAsync"/>) in addition to raw element queries (<see cref="QueryAsync"/>,
/// <see cref="QueryAllAsync"/>).
/// </para>
/// <para>
/// <b>Timeout semantics</b>: If a method accepts a <c>timeout</c> parameter and it is <c>null</c>, the
/// session’s <see cref="DefaultTimeout"/> is used. A timeout of <see cref="TimeSpan.Zero"/> performs a
/// single probe without waiting.
/// </para>
/// <para>
/// <b>Thread-safety</b>: Sessions are not intended for concurrent operations that target the same UI at once.
/// Await one operation before issuing the next to avoid focus contention and driver-specific races.
/// </para>
/// </remarks>
/// <seealso cref="Selector"/>
/// <seealso cref="IElement"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="Expect"/>
public interface IAppSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the default timeout applied by this session when per-call timeouts are not specified.
    /// </summary>
    /// <remarks>
    /// Implementations typically default this to a few seconds (e.g., 5 s). Callers may override per
    /// operation by passing an explicit timeout.
    /// </remarks>
    TimeSpan DefaultTimeout { get; }

    /// <summary>
    /// Resolves a single element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Selector describing the target element.</param>
    /// <param name="timeout">
    /// Optional overall timeout. If <c>null</c>, <see cref="DefaultTimeout"/> is used. If zero, a single probe is performed.
    /// </param>
    /// <param name="ct">A token to observe while waiting.</param>
    /// <returns>The first matching <see cref="IElement"/>.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if no matching element is found within the effective timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// This method does not cache handles; each call re-queries the UI. For Playwright-style ergonomics with
    /// auto-waiting actions, consider using <see cref="ILocator"/> produced by a driver factory.
    /// </remarks>
    Task<IElement> QueryAsync(
        Selector selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves all elements matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Selector describing the target elements.</param>
    /// <param name="timeout">
    /// Optional overall timeout. If <c>null</c>, <see cref="DefaultTimeout"/> is used. If zero, a single probe is performed.
    /// </param>
    /// <param name="ct">A token to observe while waiting.</param>
    /// <returns>
    /// A read-only list of matching elements (empty if none found within the effective timeout).
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Unlike <see cref="QueryAsync"/>, this method does not throw on timeout; it returns an empty list when no matches are found.
    /// </remarks>
    Task<IReadOnlyList<IElement>> QueryAllAsync(
        Selector selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Clicks an element resolved by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Selector for the element to click.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the click has been performed.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be located or is not clickable within <see cref="DefaultTimeout"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// This is a convenience wrapper around <see cref="QueryAsync"/> followed by <see cref="IElement.ClickAsync"/>.
    /// For richer control (custom timeouts, polling, assertions) use an <see cref="ILocator"/> and its methods.
    /// </remarks>
    Task ClickAsync(
        Selector selector,
        CancellationToken ct = default);

    /// <summary>
    /// Types text into an element resolved by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Selector for the element to receive text.</param>
    /// <param name="text">The text to enter. Empty string may clear when supported by the control.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the text has been entered.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be located or does not become editable within <see cref="DefaultTimeout"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Implementations should prefer a native text/value API when available and fall back to synthesized input otherwise.
    /// </remarks>
    Task TypeAsync(
        Selector selector,
        string text,
        CancellationToken ct = default);

    /// <summary>
    /// Attempts to close the application’s main window and (optionally) dismiss save prompts.
    /// </summary>
    /// <param name="discard">
    /// When <c>true</c>, common “Save changes?” prompts are dismissed via a “Don’t Save”/“Discard” action when present.
    /// </param>
    /// <param name="timeout">
    /// Optional overall timeout for the close/dismiss sequence. If <c>null</c>, <see cref="DefaultTimeout"/> is used.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the application has been closed (best-effort).</returns>
    /// <remarks>
    /// <para>
    /// Implementations should attempt a graceful close (e.g., WindowPattern, title-bar close), then dismiss
    /// save prompts if <paramref name="discard"/> is <c>true</c>. As a last resort, they may terminate classic
    /// standalone processes but should avoid killing shared host processes used by packaged apps.
    /// </para>
    /// <para>
    /// This method is best-effort and should not throw if the app has already exited.
    /// </para>
    /// </remarks>
    Task CloseAsync(
        bool discard = true,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}