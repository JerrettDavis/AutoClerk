using AutoClerk.Abstractions;
using FlaUI.Core;
using FlaUI.UIA2;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// FlaUI-backed implementation of <see cref="IUiDriver"/> that launches or attaches to apps
/// and produces <see cref="IAppSession"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This driver creates sessions that use a <b>lazy root discovery</b> strategy (see
/// <see cref="FlaUiAppSession"/>) to improve reliability with packaged/Store apps on Windows 10/11.
/// </para>
/// <para>
/// Returning from <see cref="LaunchAsync"/> or <see cref="AttachAsync(int, UiDriverOptions?, CancellationToken)"/>
/// indicates the process has started / been attached; it does not guarantee that a main window is immediately
/// available. Use session-level queries or expectations to wait for UI readiness.
/// </para>
/// </remarks>
/// <seealso cref="IUiDriver"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="UiDriverOptions"/>
/// <seealso cref="FlaUiAppSession"/>
public sealed class FlaUiDriver : IUiDriver
{
    /// <summary>
    /// Launches a new process and returns a live automation session attached to it.
    /// </summary>
    /// <param name="exePath">Path or execution alias for the executable to launch (e.g., <c>notepad.exe</c>).</param>
    /// <param name="args">Optional command-line arguments.</param>
    /// <param name="options">
    /// Optional driver options (backend selection, default timeout, main-window selector hint).
    /// If <c>null</c>, sensible defaults are used (<see cref="UiBackend.UIA3"/>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="IAppSession"/> bound to the launched process.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exePath"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the executable cannot be found (when a full path is required).</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled before launch.</exception>
    /// <remarks>
    /// For packaged inbox apps (e.g., Windows 11 Notepad/Calculator), prefer launching by AUMID using
    /// <see cref="DriverStoreAppExtensions.LaunchStoreAppAsync(IUiDriver, string, UiDriverOptions?, CancellationToken)"/>.
    /// </remarks>
    public Task<IAppSession> LaunchAsync(
        string exePath,
        string? args = null,
        UiDriverOptions? options = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            throw new ArgumentNullException(nameof(exePath));
        ct.ThrowIfCancellationRequested();

        options ??= new UiDriverOptions();

        var app = args is null
            ? Application.Launch(exePath)
            : Application.Launch(exePath, args);

        return CreateSessionAsync(app, options, ct);
    }

    /// <summary>
    /// Attaches to an already running process and returns a live automation session.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="options">
    /// Optional driver options (backend selection, default timeout, main-window selector hint).
    /// If <c>null</c>, sensible defaults are used (<see cref="UiBackend.UIA3"/>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="IAppSession"/> attached to the specified process.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="processId"/> is less than or equal to zero.</exception>
    /// <exception cref="ArgumentException">Thrown if the process does not exist or has already exited.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled before attach.</exception>
    public Task<IAppSession> AttachAsync(
        int processId,
        UiDriverOptions? options = null,
        CancellationToken ct = default)
    {
        if (processId <= 0)
            throw new ArgumentOutOfRangeException(nameof(processId), "Process ID must be a positive integer.");
        ct.ThrowIfCancellationRequested();

        options ??= new UiDriverOptions();

        var app = Application.Attach(processId);
        return CreateSessionAsync(app, options, ct);
    }

    /// <summary>
    /// Creates a new <see cref="FlaUiAppSession"/> for the given application with the requested backend.
    /// </summary>
    /// <param name="app">The FlaUI application handle.</param>
    /// <param name="options">Session options (backend, default timeout, main-window hint).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ready <see cref="IAppSession"/> (lazy-rooting defers main-window resolution to first query).</returns>
    /// <remarks>
    /// This method does not block on main-window handle creation; see <see cref="FlaUiAppSession"/> for details.
    /// </remarks>
    private static Task<IAppSession> CreateSessionAsync(
        Application app,
        UiDriverOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        AutomationBase automation = options.Backend switch
        {
            UiBackend.UIA2 => new UIA2Automation(),
            _ => new UIA3Automation()
        };

        return Task.FromResult<IAppSession>(new FlaUiAppSession(app, automation, options));
    }
}