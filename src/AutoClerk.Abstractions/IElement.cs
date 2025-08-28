namespace AutoClerk.Abstractions;

/// <summary>
/// Represents a resolved UI element handle within an <see cref="IAppSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ILocator"/>, which re-resolves targets per action, <see cref="IElement"/> refers to a
/// specific element instance. Implementations should guard against staleness and expose best-effort actions
/// using native control patterns when available (e.g., Invoke, Value, Text), falling back to synthesized input
/// when appropriate.
/// </para>
/// <para><b>Thread-safety:</b> Do not invoke multiple actions concurrently on the same element instance.</para>
/// </remarks>
/// <seealso cref="IAppSession"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="Selector"/>
public interface IElement
{
    /// <summary>
    /// Invokes a click on this element.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the click has been performed.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the underlying session was disposed.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the driver cannot synthesize a click for this element on the current platform.
    /// </exception>
    /// <remarks>
    /// Implementations should prefer a native invoke/action pattern when available and otherwise use a
    /// synthesized pointer click after ensuring the element is in view. To wait until the element becomes
    /// interactable, call <see cref="WaitForVisibleAsync"/> first.
    /// </remarks>
    Task ClickAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the textual value of this element.
    /// </summary>
    /// <param name="text">The text to assign. Passing <see cref="string.Empty"/> may clear the control when supported.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the text has been applied.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the underlying session was disposed.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the element does not support programmatic text input on this platform and no safe fallback exists.
    /// </exception>
    /// <remarks>
    /// Implementations should prefer a native value/text API (e.g., a “Value” pattern), and only fall back to
    /// synthesized typing as a last resort, after focusing the control.
    /// </remarks>
    Task SetTextAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Gets the current visible text/value exposed by the element.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A task that resolves to the current textual content, or <c>null</c> if the control does not expose text.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Implementations should prefer a native text/value API and fall back to the element’s accessible name when necessary.
    /// </remarks>
    Task<string?> GetTextAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks whether the element exists (and optionally waits for it to appear).
    /// </summary>
    /// <param name="timeout">
    /// Optional overall timeout to wait for existence. If <c>null</c>, a driver default may be used.
    /// If <see cref="TimeSpan.Zero"/>, the method performs a single non-blocking probe.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A task that resolves to <c>true</c> if the element exists within the effective timeout; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// This method does not throw on timeout; it returns <c>false</c> if the element cannot be validated in time.
    /// </remarks>
    Task<bool> ExistsAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Waits until the element becomes visible (present and not off-screen).
    /// </summary>
    /// <param name="timeout">
    /// Optional overall timeout. If <c>null</c>, a driver default may be used. If zero, a single probe is performed.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the element is visible.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the element does not become visible within the effective timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    Task WaitForVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a value indicating whether the element is currently enabled (interactable).
    /// </summary>
    /// <remarks>
    /// If the platform does not expose an explicit “enabled” property, implementations may infer this from available
    /// patterns and state. The value reflects the state at the time of access and is not polled.
    /// </remarks>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the element is currently visible (not off-screen).
    /// </summary>
    /// <remarks>
    /// If the platform does not expose visibility directly, implementations may infer this from “off-screen” flags or
    /// layout heuristics. The value reflects the state at the time of access and is not polled.
    /// </remarks>
    bool IsVisible { get; }
}