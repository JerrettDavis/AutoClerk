namespace AutoClerk.Abstractions;

/// <summary>
/// Selects the underlying UI automation backend used by the driver.
/// </summary>
/// <remarks>
/// <para>
/// On Windows, <see cref="UIA3"/> targets the UI Automation v3 provider (modern XAML/packaged apps usually expose
/// richer patterns via UIA3). <see cref="UIA2"/> targets the legacy provider and can be helpful on older/interop
/// surfaces. Choose the backend that best matches the application technology under test.
/// </para>
/// </remarks>
/// <seealso cref="UiDriverOptions"/>
public enum UiBackend
{
    /// <summary>
    /// UI Automation v3 (recommended default on modern Windows).
    /// </summary>
    UIA3,

    /// <summary>
    /// UI Automation v2 (legacy provider; sometimes exposes different patterns on classic Win32 surfaces).
    /// </summary>
    UIA2
}

/// <summary>
/// Options that control session-level behavior for a UI driver.
/// </summary>
/// <param name="Backend">
/// The automation backend to use (e.g., <see cref="UiBackend.UIA3"/>). Defaults to <see cref="UiBackend.UIA3"/>.
/// </param>
/// <param name="DefaultTimeout">
/// The session default timeout used when per-call timeouts are not supplied (e.g., by locators/expectations).
/// If <c>null</c>, the driver chooses a sensible default (typically ~5 seconds).
/// </param>
/// <param name="MainWindowSelector">
/// An optional selector hint to identify the application’s main window/root scope. This is especially useful for
/// packaged/Store apps where process handles can change during startup. When provided, the driver may use this
/// hint to “re-root” queries to the correct top-level window quickly and reliably.
/// </param>
/// <remarks>
/// <para>
/// <b>Scope</b>: These options apply at session creation time (e.g., <see cref="IUiDriver.LaunchAsync"/> or
/// <see cref="IUiDriver.AttachAsync"/>) and are not meant for per-action overrides. Use
/// <see cref="LocatorOptions"/> for per-call behavior.
/// </para>
/// <para>
/// <b>Immutability</b>: This is an immutable record intended to be constructed inline at call sites.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Launch with UIA3 and a robust main-window hint:
/// var session = await driver.LaunchAsync(
///     exePath: "myapp.exe",
///     options: new UiDriverOptions(
///         Backend: UiBackend.UIA3,
///         DefaultTimeout: TimeSpan.FromSeconds(8),
///         MainWindowSelector: Selector.XPath("//Window[contains(@Name,'MyApp')]")));
/// </code>
/// </example>
/// <seealso cref="IUiDriver"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="LocatorOptions"/>
public sealed record UiDriverOptions(
    UiBackend Backend = UiBackend.UIA3,
    TimeSpan? DefaultTimeout = null,
    Selector? MainWindowSelector = null
);