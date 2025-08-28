namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Lightweight async spin-wait utility for repeatedly probing an asynchronous condition until it succeeds,
/// fails, or times out.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Retry"/> is designed for very short, frequent probes (tens of milliseconds between attempts)
/// where allocating full-blown wait handles or complex retry policies would be overkill. It is used
/// internally to implement Playwright-style auto-waits in the driver layer.
/// </para>
/// <para>
/// <b>Semantics</b>:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>The <paramref name="probe"/> function is awaited on each iteration; its result is passed to
///     <paramref name="ok"/>. If <paramref name="ok"/> returns <c>true</c>, the value is returned immediately.</description>
///   </item>
///   <item>
///     <description>If <paramref name="probe"/> throws, the exception is remembered and the loop continues until
///     timeout/cancellation. On timeout, the <b>last</b> exception is rethrown (if any); otherwise a
///     <see cref="TimeoutException"/> is thrown.</description>
///   </item>
///   <item>
///     <description><paramref name="poll"/> controls the delay between attempts. Choose a value that balances
///     responsiveness and CPU usage (e.g., 50–100 ms for UI polling).</description>
///   </item>
/// </list>
/// <para>
/// <b>Thread-safety:</b> This type holds no state; it is safe to use concurrently from multiple callers.
/// </para>
/// </remarks>
internal static class Retry
{
    /// <summary>
    /// Repeatedly executes an asynchronous <paramref name="probe"/> until <paramref name="ok"/> returns <c>true</c>,
    /// the <paramref name="timeout"/> elapses, or <paramref name="ct"/> is canceled.
    /// </summary>
    /// <typeparam name="T">The result type produced by <paramref name="probe"/>.</typeparam>
    /// <param name="probe">An asynchronous function that produces the value to test.</param>
    /// <param name="ok">A predicate that determines whether the probed value satisfies the condition.</param>
    /// <param name="timeout">Overall time budget for the operation.</param>
    /// <param name="poll">Delay between attempts. Smaller values increase responsiveness at the cost of CPU.</param>
    /// <param name="ct">Cancellation token to abort the wait promptly.</param>
    /// <returns>The last value produced by <paramref name="probe"/> that satisfied <paramref name="ok"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="probe"/> or <paramref name="ok"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the timeout elapses without success and no <paramref name="probe"/> exception occurred on the last attempt.
    /// </exception>
    /// <exception cref="Exception">
    /// Rethrows the <b>last</b> exception thrown by <paramref name="probe"/> if any occurred during the polling window.
    /// </exception>
    /// <remarks>
    /// <para>
    /// For best results, keep <paramref name="probe"/> side-effect free and idempotent; retries may invoke it many times.
    /// </para>
    /// <para>
    /// Example (waiting for an element to appear quickly):
    /// </para>
    /// <code>
    /// var el = await Retry.SpinWaitAsync(
    ///     probe: async () =&gt; await session.QueryAsync(selector, timeout: TimeSpan.FromMilliseconds(50), ct),
    ///     ok: _ =&gt; true,
    ///     timeout: TimeSpan.FromSeconds(5),
    ///     poll: TimeSpan.FromMilliseconds(50),
    ///     ct: ct);
    /// </code>
    /// </remarks>
    public static async Task<T> SpinWaitAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> ok,
        TimeSpan timeout,
        TimeSpan poll,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(ok);

        var deadline = DateTime.UtcNow + timeout;
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var v = await probe().ConfigureAwait(false);
                if (ok(v)) return v;
            }
            catch (Exception ex)
            {
                last = ex;
            }

            await Task.Delay(poll, ct).ConfigureAwait(false);
        }

        if (last is not null) throw last;
        throw new TimeoutException($"Operation timed out after {timeout.TotalMilliseconds:0} ms.");
    }
}