namespace AutoClerk.Abstractions;

/// <summary>
/// Creates and attaches automation sessions to UI applications.
/// </summary>
/// <remarks>
/// <para>
/// A driver abstracts the platform-specific mechanics of starting or attaching to an application and producing an
/// <see cref="IAppSession"/> for subsequent queries and interactions. The session returned by these methods is
/// <see cref="IAsyncDisposable"/> and should be disposed when no longer needed to release platform resources and
/// close the target app if appropriate.
/// </para>
///
/// <para><b>Readiness</b></para>
/// <para>
/// Returning from <see cref="LaunchAsync"/> or <see cref="AttachAsync"/> indicates the process is started/attached,
/// not necessarily that a main window or target control is immediately available. Callers should use session-level
/// waits (e.g., <see cref="IAppSession.QueryAsync(Selector, TimeSpan?, CancellationToken)"/> or
/// locator expectations) to wait for UI readiness.
/// </para>
///
/// <para><b>Platform &amp; packaged apps</b></para>
/// <para>
/// Specific drivers may provide additional entry points to launch packaged or store-delivered apps (e.g., by AppUserModelID).
/// Such conveniences are driver-specific extensions and are intentionally not part of this abstraction.
/// </para>
/// </remarks>
/// <seealso cref="IAppSession"/>
/// <seealso cref="UiDriverOptions"/>
/// <seealso cref="Selector"/>
public interface IUiDriver
{
    /// <summary>
    /// Launches a new application process and returns a live automation session attached to it.
    /// </summary>
    /// <param name="exePath">
    /// Path to the executable to launch. Drivers may also honor execution aliases supported by the host OS.
    /// </param>
    /// <param name="args">Optional command-line arguments passed to the process.</param>
    /// <param name="options">
    /// Optional driver options (e.g., backend selection, default timeouts, main-window selection hints).
    /// If <c>null</c>, driver defaults are used.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IAppSession"/> for the launched process.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exePath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Thrown if the executable cannot be found (when a full path is required).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the process starts but cannot be attached by the driver (e.g., incompatible process model).
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// The returned session may not have a visible window yet. Use element queries or expectations to wait for UI state.
    /// </remarks>
    Task<IAppSession> LaunchAsync(
        string exePath,
        string? args = null,
        UiDriverOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Attaches to an already-running application process and returns a live automation session.
    /// </summary>
    /// <param name="processId">The target process ID to attach to.</param>
    /// <param name="options">
    /// Optional driver options (e.g., backend selection, default timeouts, main-window selection hints).
    /// If <c>null</c>, driver defaults are used.
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IAppSession"/> attached to the specified process.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="processId"/> is not a positive integer.</exception>
    /// <exception cref="ArgumentException">Thrown if the process does not exist or has already exited.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the driver cannot attach to the process (e.g., insufficient permissions or incompatible process model).
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Attaching does not guarantee the presence of a main window; use session queries/expectations to wait for UI readiness.
    /// </remarks>
    Task<IAppSession> AttachAsync(
        int processId,
        UiDriverOptions? options = null,
        CancellationToken ct = default);
}