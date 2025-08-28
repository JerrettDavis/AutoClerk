using AutoClerk.Abstractions;
using FlaUI.Core;
using FlaUI.UIA2;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Extension methods for launching packaged (Store) apps by AppUserModelID (AUMID).
/// </summary>
/// <remarks>
/// <para>
/// On Windows 10/11, many inbox apps (e.g., Calculator, Notepad) are distributed as packaged apps.
/// Launching those by file name (e.g., <c>calc.exe</c>) can yield a stub process or delayed main window.
/// Launching by AUMID is the reliable, supported approach.
/// </para>
/// <para>
/// Examples of AUMIDs:
/// </para>
/// <list type="bullet">
///   <item><description><c>Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c></description></item>
///   <item><description><c>Microsoft.WindowsNotepad_8wekyb3d8bbwe!App</c></description></item>
/// </list>
/// <para>
/// The returned <see cref="IAppSession"/> is driver-backed and should be disposed (e.g., via <c>await using</c>)
/// to free resources and close the target app when appropriate. Consider providing a
/// <see cref="UiDriverOptions.MainWindowSelector"/> hint (e.g., an XPath matching the window title) to help the
/// driver quickly bind to the real top-level window on systems where the host process changes during startup.
/// </para>
/// </remarks>
/// <seealso cref="IUiDriver"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="UiDriverOptions"/>
public static class DriverStoreAppExtensions
{
    /// <summary>
    /// Launches a packaged (Store) app by its AppUserModelID (AUMID) and returns a live automation session.
    /// </summary>
    /// <param name="driver">The UI driver used to create the session (receiver of this extension method).</param>
    /// <param name="aumid">
    /// The AppUserModelID of the application to launch (e.g., <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c>).
    /// </param>
    /// <param name="options">
    /// Optional driver options (backend selection, default timeout, and an optional <see cref="UiDriverOptions.MainWindowSelector"/> hint).
    /// If <c>null</c>, sensible defaults are used (UIA3 backend, driver default timeout).
    /// </param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IAppSession"/> attached to the launched packaged app.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="driver"/> or <paramref name="aumid"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="aumid"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// Returning from this method indicates the app has been launched; a visible window may still be materializing.
    /// Use session-level queries/expectations to await UI readiness.
    /// </para>
    /// <para>
    /// Backend selection:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="UiBackend.UIA3"/> (default) is recommended on modern Windows and XAML-based apps.</description></item>
    ///   <item><description><see cref="UiBackend.UIA2"/> can be useful on some classic/interop surfaces.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new UiDriverOptions(
    ///     Backend: UiBackend.UIA3,
    ///     DefaultTimeout: TimeSpan.FromSeconds(10),
    ///     MainWindowSelector: Selector.XPath("//Window[contains(@Name,'Calculator')]"));
    ///
    /// await using var session = await driver.LaunchStoreAppAsync(
    ///     "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", options);
    ///
    /// // Wait for the results field:
    /// var factory = new FlaUiLocatorFactory();
    /// var results = session.Locator(factory, Selector.XPath("//*[@AutomationId='CalculatorResults']"));
    /// await Expect.That(results).ToBeVisibleAsync();
    /// </code>
    /// </example>
    public static async Task<IAppSession> LaunchStoreAppAsync(
        this IUiDriver driver,
        string aumid,
        UiDriverOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(aumid);
        ct.ThrowIfCancellationRequested();

        options ??= new UiDriverOptions();

        // Launch the packaged app (AUMID).
        var app = Application.LaunchStoreApp(aumid);

        // Pick backend and build session.
        AutomationBase automation = options.Backend == UiBackend.UIA2
            ? new UIA2Automation()
            : new UIA3Automation();

        return await Task.FromResult<IAppSession>(new FlaUiAppSession(app, automation, options))
            .ConfigureAwait(false);
    }
}