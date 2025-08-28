namespace AutoClerk.Abstractions;

/// <summary>
/// Per-call overrides that control auto-waiting behavior for locator actions and expectations.
/// </summary>
/// <remarks>
/// <para>
/// These options apply only to the specific call where they are provided and do not mutate session defaults.
/// When an option is <c>null</c> or not set, the underlying driver/session chooses a sensible default
/// (see each property’s documentation).
/// </para>
/// <para>
/// <b>Precedence</b>:
/// <list type="number">
///   <item>
///     <description>Explicit <see cref="LocatorOptions"/> passed to a method.</description>
///   </item>
///   <item>
///     <description><see cref="IAppSession.DefaultTimeout"/> and driver defaults.</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Immutability</b>: This type is designed for ephemeral, per-call use. Create new instances as needed; do not reuse and mutate across calls.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Fast, single-probe visibility check (no waiting):
/// var fast = new LocatorOptions { Timeout = TimeSpan.Zero };
/// bool visible = await button.IsVisibleAsync(fast);
///
/// // Longer, fine-grained wait with tighter polling:
/// var wait = new LocatorOptions { Timeout = TimeSpan.FromSeconds(8), PollInterval = TimeSpan.FromMilliseconds(50) };
/// await Expect.That(dialog).ToBeVisibleAsync(wait);
///
/// // Non-fatal expectation (no exception on timeout):
/// var trial = new LocatorOptions { Timeout = TimeSpan.FromSeconds(2), Trial = true };
/// await Expect.That(status).ToHaveTextAsync("Ready", trial); // completes quietly if not matched
/// </code>
/// </example>
/// <seealso cref="IAppSession"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="Expect"/>
public sealed class LocatorOptions
{
    /// <summary>
    /// Overall timeout for the action or check.
    /// </summary>
    /// <value>
    /// If <c>null</c>, the driver uses <see cref="IAppSession.DefaultTimeout"/>. A value of <see cref="TimeSpan.Zero"/>
    /// requests a single non-blocking probe (no waiting). Negative values are treated as <see cref="TimeSpan.Zero"/>.
    /// </value>
    /// <remarks>
    /// Applies to the full operation, including element resolution and any readiness checks (e.g., visibility, enabled state).
    /// </remarks>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Polling cadence while waiting for readiness.
    /// </summary>
    /// <value>
    /// If <c>null</c>, the driver selects a sensible default (typically 50–100 ms). Values less than 10 ms may increase CPU usage
    /// without meaningful latency improvements and can be clamped by the driver.
    /// </value>
    /// <remarks>
    /// This interval controls how frequently the driver re-probes for the desired condition (existence, visibility, enabled, text match, etc.).
    /// It does not affect synthesized input speed.
    /// </remarks>
    public TimeSpan? PollInterval { get; init; }

    /// <summary>
    /// Trial mode for expectations.
    /// </summary>
    /// <value>
    /// If <c>true</c>, expectation-style helpers (e.g., <c>Expect.That(locator).ToBeVisibleAsync(...)</c>) will complete
    /// without throwing on timeout; they indicate failure via their return semantics instead. Action methods may still throw
    /// on hard failures (e.g., unsupported patterns, input synthesis errors).
    /// </value>
    /// <remarks>
    /// Use trial mode to perform non-fatal checks inside broader flows (e.g., optional banners, transient toasts).
    /// </remarks>
    public bool Trial { get; init; }
}