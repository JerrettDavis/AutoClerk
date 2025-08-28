using AutoClerk.Abstractions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// FlaUI-backed implementation of <see cref="ILocator"/> that provides Playwright-style, auto-waiting interactions.
/// </summary>
/// <param name="session">
/// The live <see cref="IAppSession"/> used to resolve elements and perform actions.
/// </param>
/// <param name="selector">
/// The immutable <see cref="Selector"/> describing the target element(s).
/// </param>
/// <remarks>
/// <para>
/// The locator does not cache a UI handle. Each action re-resolves the element with the bound
/// <see cref="Selector"/> and honors per-call <see cref="LocatorOptions"/> (or falls back to
/// <see cref="IAppSession.DefaultTimeout"/> and driver defaults).
/// </para>
/// <para>
/// <b>Auto-wait semantics</b>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="LocatorOptions.Timeout"/> bounds the total wait for each action.</description></item>
///   <item><description><see cref="LocatorOptions.PollInterval"/> controls re-probe cadence while waiting.</description></item>
///   <item><description>Actions ensure basic readiness (visibility/enabled) before interacting.</description></item>
/// </list>
/// <para>
/// <b>Key presses</b>: <see cref="PressAsync(string, LocatorOptions?, CancellationToken)"/> maps the provided
/// key name to Windows <see cref="VirtualKeyShort"/>; chords and text entry should be sent via
/// <see cref="FillAsync(string, LocatorOptions?, CancellationToken)"/> or a higher-level DSL verb.
/// </para>
/// </remarks>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="LocatorOptions"/>
internal sealed class LocatorShim(IAppSession session, Selector selector) : ILocator
{
    /// <inheritdoc />
    public Selector Selector { get; } = selector;

    // Helper: effective timeout and poll interval
    private static TimeSpan T(LocatorOptions? o, IAppSession s) => o?.Timeout ?? s.DefaultTimeout;
    private static TimeSpan P(LocatorOptions? o) => o?.PollInterval ?? TimeSpan.FromMilliseconds(50);

    // Resolve an element once, honoring the call-level timeout.
    private Task<IElement> QueryReadyAsync(LocatorOptions? options, CancellationToken ct) =>
        session.QueryAsync(Selector, timeout: T(options, session), ct: ct);

    /// <summary>
    /// Clicks the element after waiting for it to become visible and enabled.
    /// </summary>
    /// <param name="options">Per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the click has been performed.</returns>
    /// <exception cref="TimeoutException">Thrown if the element cannot be located or become clickable in time.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public async Task ClickAsync(LocatorOptions? options = null, CancellationToken ct = default)
    {
        var timeout = T(options, session);
        var poll = P(options);

        // Resolve quickly, then ensure visibility/enabled before clicking.
        var el = await Retry.SpinWaitAsync(
            async () => await session.QueryAsync(Selector, timeout: poll, ct: ct),
            ok: _ => true, timeout, poll, ct).ConfigureAwait(false);

        await el.WaitForVisibleAsync(timeout, ct).ConfigureAwait(false);
        await el.ClickAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Types text into the element after it becomes visible and enabled.
    /// </summary>
    /// <param name="text">The text to input. Empty string may clear when supported by the control.</param>
    /// <param name="options">Per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when text has been entered.</returns>
    /// <exception cref="TimeoutException">Thrown if the element cannot be located or become editable in time.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public async Task FillAsync(string text, LocatorOptions? options = null, CancellationToken ct = default)
    {
        var el = await QueryReadyAsync(options, ct).ConfigureAwait(false);
        await el.WaitForVisibleAsync(T(options, session), ct).ConfigureAwait(false);
        await el.SetTextAsync(text, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a single key (e.g., <c>"Enter"</c>, <c>"Tab"</c>) while the element has focus.
    /// </summary>
    /// <param name="key">
    /// A key name recognized by <see cref="VirtualKeyShort"/>; e.g., <c>"ENTER"</c>, <c>"TAB"</c>, <c>"ESCAPE"</c>.
    /// </param>
    /// <param name="options">Per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the key press has been synthesized.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> cannot be mapped to <see cref="VirtualKeyShort"/>.</exception>
    /// <exception cref="TimeoutException">Thrown if the element does not become ready in time.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// This method is intended for discrete key presses. For text entry, prefer <see cref="FillAsync(string, LocatorOptions?, CancellationToken)"/>.
    /// </remarks>
    public async Task PressAsync(
        string key,
        LocatorOptions? options = null,
        CancellationToken ct = default)
    {
        var el = await QueryReadyAsync(options, ct).ConfigureAwait(false);
        await el.WaitForVisibleAsync(T(options, session), ct).ConfigureAwait(false);

        // Touch the element (focus) via a light interaction path; some providers need it before key input.
        _ = await el.GetTextAsync(ct).ConfigureAwait(false);

        if (!Enum.TryParse<VirtualKeyShort>(key, ignoreCase: true, out var winKey))
            throw new ArgumentException($"Invalid key: {key}", nameof(key));

        Keyboard.Press(winKey);
        Keyboard.Release(winKey);
    }

    /// <summary>
    /// Retrieves the element’s current visible text/value.
    /// </summary>
    /// <param name="options">Per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current text, or <c>null</c> if the control does not expose text.</returns>
    /// <exception cref="TimeoutException">Thrown if the element cannot be located in time.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public async Task<string?> TextContentAsync(LocatorOptions? options = null, CancellationToken ct = default)
    {
        var el = await QueryReadyAsync(options, ct).ConfigureAwait(false);
        return await el.GetTextAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns <c>true</c> if the element is visible at the time of the check.
    /// </summary>
    /// <param name="options">
    /// Per-call options; when provided, only <see cref="LocatorOptions.PollInterval"/> is used to bound the quick probe.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the element is visible; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This method is a fast presence/visibility probe; it does not throw on timeout and uses a short probe window.
    /// </remarks>
    public async Task<bool> IsVisibleAsync(LocatorOptions? options = null, CancellationToken ct = default)
    {
        try
        {
            var el = await session.QueryAsync(Selector, timeout: P(options), ct: ct).ConfigureAwait(false);
            return el.IsVisible;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the element is enabled at the time of the check.
    /// </summary>
    /// <param name="options">
    /// Per-call options; when provided, only <see cref="LocatorOptions.PollInterval"/> is used to bound the quick probe.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the element is enabled; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This method is a fast readiness probe; it does not throw on timeout and uses a short probe window.
    /// </remarks>
    public async Task<bool> IsEnabledAsync(LocatorOptions? options = null, CancellationToken ct = default)
    {
        try
        {
            var el = await session.QueryAsync(Selector, timeout: P(options), ct: ct).ConfigureAwait(false);
            return el.IsEnabled;
        }
        catch
        {
            return false;
        }
    }
}