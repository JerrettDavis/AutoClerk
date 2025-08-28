namespace AutoClerk.Abstractions;

/// <summary>
/// A Playwright-style locator bound to a <see cref="Selector"/> and an application session.
/// </summary>
/// <remarks>
/// <para>
/// A locator does not cache a UI handle. Each action (e.g., <see cref="ClickAsync"/>, <see cref="FillAsync"/>) will
/// (re)resolve the element using the bound <see cref="Selector"/> and apply built-in auto-waiting until the element
/// is ready for the requested interaction. This makes locators resilient to dynamic UIs.
/// </para>
/// <para>
/// <b>Auto-wait semantics</b>:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><see cref="LocatorOptions.Timeout"/> controls the overall wait for the target element to appear
///     and become actionable. If <c>null</c>, the driver uses <see cref="IAppSession.DefaultTimeout"/>.</description>
///   </item>
///   <item>
///     <description><see cref="LocatorOptions.PollInterval"/> controls how often the driver probes for readiness.
///     Implementations typically default to ~50–100 ms.</description>
///   </item>
///   <item>
///     <description>If <see cref="LocatorOptions.Timeout"/> is <see cref="TimeSpan.Zero"/>, the operation becomes
///     a single non-blocking probe (no waiting). This is useful for fast presence checks.</description>
///   </item>
/// </list>
/// <para>
/// <b>Thread-safety</b>: Instances are lightweight and immutable, but individual actions are not intended to run
/// concurrently on the same locator. Await one action before starting the next.
/// </para>
/// <para>
/// <b>Platform</b>: Behavior depends on the underlying driver. For example, a Windows UIAutomation-based driver may use
/// Invoke/Value/Text patterns when available and fall back to synthesized input when necessary.
/// </para>
/// </remarks>
/// <seealso cref="Selector"/>
/// <seealso cref="LocatorOptions"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="Expect"/>
public interface ILocator
{
    /// <summary>
    /// Gets the <see cref="Selector"/> this locator is bound to.
    /// </summary>
    /// <remarks>
    /// This value is immutable and used on each action to re-resolve the current element instance.
    /// </remarks>
    Selector Selector { get; }

    /// <summary>
    /// Clicks the element.
    /// </summary>
    /// <param name="options">
    /// Optional per-call options (timeout, polling, and trial mode). If <c>null</c>, defaults are used.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the element and performing the click.
    /// </param>
    /// <returns>A task that completes when the click has been performed.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be located or is not clickable within the effective timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if <paramref name="ct"/> is canceled before completion.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the underlying session has been disposed before the action completes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the driver cannot synthesize a click for the resolved element (e.g., platform limitation).
    /// </exception>
    /// <example>
    /// <code>
    /// var login = app.Locator(Selector.Id("LoginButton"));
    /// await login.ClickAsync(new LocatorOptions { Timeout = TimeSpan.FromSeconds(5) }, ct);
    /// </code>
    /// </example>
    Task ClickAsync(LocatorOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Types text into the element.
    /// </summary>
    /// <param name="text">The text to input. Empty string clears when supported by the target control.</param>
    /// <param name="options">Optional per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the text has been entered.</returns>
    /// <exception cref="TimeoutException">The element did not become editable within the timeout.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled via <paramref name="ct"/>.</exception>
    /// <exception cref="NotSupportedException">The element does not support text input on this platform.</exception>
    /// <remarks>
    /// Implementations should prefer an explicit text/value API if exposed by the control; otherwise they may
    /// synthesize keyboard input after focusing the element.
    /// </remarks>
    Task FillAsync(string text, LocatorOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a key or key-chord while the element has focus (e.g., <c>"Enter"</c>, <c>"Tab"</c>).
    /// </summary>
    /// <param name="key">
    /// The key identifier. Accepted values are driver-specific; common names include <c>"Enter"</c>, <c>"Tab"</c>,
    /// <c>"Esc"</c>. Chords such as <c>"Ctrl+S"</c> may be supported depending on the driver.
    /// </param>
    /// <param name="options">Optional per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the key sequence has been sent.</returns>
    /// <exception cref="TimeoutException">The element did not become focusable within the timeout.</exception>
    /// <exception cref="ArgumentException">The <paramref name="key"/> format is not recognized by the driver.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled via <paramref name="ct"/>.</exception>
    Task PressAsync(string key, LocatorOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the element's current visible text or value.
    /// </summary>
    /// <param name="options">Optional per-call options (timeout/polling).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A task that resolves to the current text/value, or <c>null</c> if no text is available for the control.
    /// </returns>
    /// <exception cref="TimeoutException">The element could not be located within the timeout.</exception>
    /// <remarks>
    /// Drivers should prefer a native text API (e.g., a text/value pattern) and fall back to the element's accessible
    /// name if necessary.
    /// </remarks>
    Task<string?> TextContentAsync(LocatorOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when the element is visible (not off-screen) at the time of evaluation.
    /// </summary>
    /// <param name="options">Optional per-call options. If a timeout is provided, the method will wait until the element becomes visible or the timeout elapses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to <c>true</c> if the element is visible; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// "Visible" is defined by the driver/platform and typically means the element is present and not off-screen.
    /// No exception is thrown on timeout; the method returns <c>false</c>.
    /// </remarks>
    Task<bool> IsVisibleAsync(LocatorOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when the element is enabled (interactable) at the time of evaluation.
    /// </summary>
    /// <param name="options">Optional per-call options. If a timeout is provided, the method will wait until the element becomes enabled or the timeout elapses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to <c>true</c> if the element is enabled; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// No exception is thrown on timeout; the method returns <c>false</c>.
    /// </remarks>
    Task<bool> IsEnabledAsync(LocatorOptions? options = null, CancellationToken ct = default);
}