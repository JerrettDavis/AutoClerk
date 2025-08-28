using System.Diagnostics;
using AutoClerk.Abstractions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Application = FlaUI.Core.Application;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Default FlaUI-backed implementation of <see cref="IAppSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// This session uses a <b>lazy root discovery</b> model: it starts from the desktop and re-acquires
/// the real top-level window on demand using an optional <see cref="UiDriverOptions.MainWindowSelector"/>
/// hint. This avoids blocking calls into process/PID-based main-window detection which can be slow or
/// unreliable for packaged/Store apps on Windows 10/11.
/// </para>
/// <para>
/// Element queries are performed with FlaUI conditions constructed from <see cref="Selector"/> and benefit
/// from resilient fallbacks (e.g., safe accessors for visibility properties that some providers don’t support).
/// </para>
/// <para>
/// <b>Thread-safety:</b> Do not issue multiple concurrent operations against the same session; await one
/// operation before starting the next to avoid focus contention and provider race conditions.
/// </para>
/// </remarks>
/// <seealso cref="IAppSession"/>
/// <seealso cref="UiDriverOptions"/>
/// <seealso cref="Selector"/>
internal sealed class FlaUiAppSession : IAppSession
{
    private readonly Application _app;
    private readonly AutomationBase _automation;
    private readonly ConditionFactory _cf;
    private readonly UiDriverOptions _options;

    // Start from Desktop, then re-acquire the real window lazily.
    private AutomationElement _root;
    private DateTime _rootLastRefresh = DateTime.MinValue;
    private static readonly TimeSpan RootTtl = TimeSpan.FromSeconds(2);

    /// <inheritdoc />
    public TimeSpan DefaultTimeout { get; }

    /// <summary>
    /// Initializes a new <see cref="FlaUiAppSession"/>.
    /// </summary>
    /// <param name="app">The FlaUI application handle backing this session.</param>
    /// <param name="automation">The FlaUI automation backend (UIA3/UIA2).</param>
    /// <param name="options">Session options (backend already chosen, timeouts, main window hint).</param>
    /// <remarks>
    /// The constructor does <b>not</b> resolve the main window eagerly. It sets the initial root to the
    /// desktop and defers window discovery to the first query, which improves startup time and reliability
    /// for packaged apps whose main handle may be created asynchronously.
    /// </remarks>
    public FlaUiAppSession(
        Application app,
        AutomationBase automation,
        UiDriverOptions options)
    {
        _app = app;
        _automation = automation;
        _options = options;
        _cf = new ConditionFactory(_automation.PropertyLibrary);
        DefaultTimeout = options.DefaultTimeout ?? TimeSpan.FromSeconds(5);
        _root = _automation.GetDesktop();
    }

    /// <summary>
    /// Ensures that the session has a fresh search root (desktop or real top-level window).
    /// </summary>
    /// <returns>The current automation element used as the search scope.</returns>
    /// <remarks>
    /// Uses <see cref="UiDriverOptions.MainWindowSelector"/> (if provided) as a strong hint. Otherwise,
    /// applies a lightweight heuristic (first visible, enabled window with “Notepad” in the title) and
    /// falls back to the desktop. Results are cached briefly (<see cref="RootTtl"/>) to avoid excess UIA calls.
    /// </remarks>
    private AutomationElement EnsureRoot()
    {
        var now = DateTime.UtcNow;
        if ((_rootLastRefresh + RootTtl) > now && _root is { IsAvailable: true })
            return _root;

        var desktop = _automation.GetDesktop();
        AutomationElement? candidate = null;

        // Prefer caller hint
        if (_options.MainWindowSelector is not null)
        {
            candidate = SelectorTranslator.FindFirst(desktop, _cf, _options.MainWindowSelector);
            if (candidate is not null)
            {
                _root = candidate;
                _rootLastRefresh = now;
                return _root;
            }
        }

        // Generic Notepad fallback (avoid fragile PID/handles)
        var wins = desktop.FindAllChildren(_cf.ByControlType(ControlType.Window));
        foreach (var w in wins)
        {
            SafeProperty.TryIsOffscreen(w, out var off);
            SafeProperty.TryIsEnabled(w, out var en);
            var name = w.Name ?? string.Empty;
            if (en && !off && name.IndexOf("Notepad", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _root = w;
                _rootLastRefresh = now;
                return _root;
            }
        }

        _root = desktop;
        _rootLastRefresh = now;
        return _root;
    }

    /// <inheritdoc />
    public async Task<IElement> QueryAsync(
        Selector selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var t = timeout ?? DefaultTimeout;
        var deadline = DateTime.UtcNow + t;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var scope = EnsureRoot();
            var el = SelectorTranslator.FindFirst(scope, _cf, selector);
            if (el is not null) return new FlaUiElement(el);
            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Element not found: {selector}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IElement>> QueryAllAsync(
        Selector selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var t = timeout ?? DefaultTimeout;
        var deadline = DateTime.UtcNow + t;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var scope = EnsureRoot();
            var els = SelectorTranslator.FindAll(scope, _cf, selector);
            if (els.Count > 0) return els.Select(e => (IElement)new FlaUiElement(e)).ToList();
            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        return Array.Empty<IElement>();
    }

    /// <inheritdoc />
    public async Task ClickAsync(
        Selector selector,
        CancellationToken ct = default)
    {
        var el = await QueryAsync(selector, ct: ct).ConfigureAwait(false);
        await el.WaitForVisibleAsync(ct: ct).ConfigureAwait(false);
        await el.ClickAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TypeAsync(
        Selector selector,
        string text,
        CancellationToken ct = default)
    {
        var el = await QueryAsync(selector, ct: ct).ConfigureAwait(false);
        await el.WaitForVisibleAsync(ct: ct).ConfigureAwait(false);
        await el.SetTextAsync(text, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The close sequence escalates through several steps:
    /// </para>
    /// <list type="number">
    ///   <item><description>Attempt graceful close via WindowPattern or title-bar “Close”.</description></item>
    ///   <item><description>If <paramref name="discard"/> is <c>true</c>, dismiss common save prompts
    ///     (e.g., “Don’t Save” / “Discard”).</description></item>
    ///   <item><description>Send a <c>WM_CLOSE</c> to the native window handle as a reliable fallback.</description></item>
    ///   <item><description>As a last resort for classic <c>notepad.exe</c>, terminate the process. Packaged hosts are not killed.</description></item>
    /// </list>
    /// <para>
    /// The method is best-effort and suppresses teardown exceptions; it is also called from <see cref="DisposeAsync"/>.
    /// </para>
    /// </remarks>
    public async Task CloseAsync(
        bool discard = true,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var t = timeout ?? TimeSpan.FromSeconds(3);
        var deadline = DateTime.UtcNow + t;
        var scope = EnsureRoot();

        // 1) WindowPattern.Close or titlebar Close
        try
        {
            var wp = scope.Patterns.Window;
            if (wp.IsSupported) wp.Pattern.Close();
            else
            {
                var closeBtn = scope.FindFirstDescendant(
                    new AndCondition(_cf.ByControlType(ControlType.Button), _cf.ByName("Close")));
                var button = closeBtn?.AsButton();
                if (button is not null) button.Invoke();
                else closeBtn?.Click();
            }
        }
        catch
        {
            /* ignore */
        }

        // 2) Dismiss save prompt
        if (discard)
        {
            try
            {
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    var desktop = _automation.GetDesktop();

                    var dontSave = desktop.FindFirstDescendant(_cf.ByName("Don't Save"))
                                   ?? desktop.FindFirstDescendant(_cf.ByName("Don’t Save"))
                                   ?? desktop.FindFirstDescendant(_cf.ByName("Discard"));
                    if (dontSave is not null)
                    {
                        var button = dontSave.AsButton();
                        if (button is not null) button.Invoke();
                        else dontSave.Click();
                        break;
                    }

                    await Task.Delay(60, ct).ConfigureAwait(false);
                }
            }
            catch
            {
                /* ignore */
            }
        }

        // 3) WM_CLOSE (reliable)
        try
        {
            if (scope.Properties.NativeWindowHandle.TryGetValue(out var hwnd) && hwnd != 0)
                NativeWin.SendMessage((IntPtr)hwnd, NativeWin.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            /* ignore */
        }

        // 4) Wait for window to go
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (!(scope?.IsAvailable ?? false)) break;
                await Task.Delay(60, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            /* ignore */
        }

        // 5) Final: kill classic notepad.exe only (avoid killing hosts)
        try
        {
            var pId = _app?.ProcessId;
            var p = (pId.HasValue && pId.Value > 0) ? Process.GetProcessById(pId.Value) : null;
            if (p is not null && !p.HasExited &&
                string.Equals(p.ProcessName, "notepad", StringComparison.OrdinalIgnoreCase) &&
                _app != null)
            {
                _app.Kill();
            }
        }
        catch
        {
            // swallow on teardown
        }
    }

    /// <summary>
    /// Disposes the session, attempting a best-effort app close and releasing automation resources.
    /// </summary>
    /// <remarks>
    /// Calls <see cref="CloseAsync(bool, TimeSpan?, CancellationToken)"/> with <c>discard: true</c> and a short timeout,
    /// then disposes the FlaUI automation and app handles. Exceptions during teardown are suppressed.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync(discard: true, timeout: TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignored
        }

        try
        {
            _automation.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _app?.Dispose();
        }
        catch
        {
            // ignored
        }
    }
}
