using AutoClerk.Abstractions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// FlaUI-backed implementation of <see cref="IElement"/> representing a concrete UI element handle.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ILocator"/>, which re-resolves targets for each action, this type holds a specific
/// <see cref="AutomationElement"/> instance. Implementations attempt native control patterns first
/// (Invoke/Value/Text/LegacyIAccessible) and fall back to synthesized input where appropriate.
/// </para>
/// <para>
/// <b>Staleness:</b> The underlying <see cref="AutomationElement"/> can become invalid if the UI re-renders or the
/// window closes. Methods attempt to fail fast in such cases. Prefer using locators for long-lived flows.
/// </para>
/// <para><b>Thread-safety:</b> Do not run actions concurrently on the same instance.</para>
/// </remarks>
/// <seealso cref="IElement"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="FlaUiAppSession"/>
internal sealed class FlaUiElement(AutomationElement el) : IElement
{
    /// <inheritdoc />
    public bool IsEnabled => el.IsEnabled;

    /// <inheritdoc />
    public bool IsVisible
    {
        get
        {
            if (el is null) return false;
            // Some providers throw when querying IsOffscreen; SafeProperty shields that.
            return !(SafeProperty.TryIsOffscreen(el, out var off) && off);
        }
    }

    /// <summary>
    /// Invokes a click on the element using native patterns when possible, otherwise a synthesized UIA click.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the click is performed.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the element is not clickable (disabled or off-screen) and no safe fallback is available.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public Task ClickAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var button = el.AsButton();
        if (button is null && !(el?.IsEnabled == true && el?.IsOffscreen == false))
            throw new InvalidOperationException("Element is not clickable (not enabled or offscreen).");

        if (button is not null)
            button.Invoke();
        else
            el?.Click();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the element’s textual value using Value/Text patterns when available; falls back to synthesized typing.
    /// </summary>
    /// <param name="text">The text to assign. Passing <see cref="string.Empty"/> may clear the control if supported.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the text is applied.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    /// <remarks>
    /// Preference order:
    /// <list type="number">
    ///   <item><description><c>TextBox.Text</c> (for Edit controls)</description></item>
    ///   <item><description><c>ValuePattern.SetValue</c></description></item>
    ///   <item><description>Focus + <see cref="Keyboard.Type(string)"/></description></item>
    /// </list>
    /// </remarks>
    public Task SetTextAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var tb = el.AsTextBox();
        if (tb is not null)
        {
            tb.Text = text;
            return Task.CompletedTask;
        }

        // Fallback: ValuePattern if not a TextBox
        var vp = el.Patterns.Value;
        if (vp.IsSupported)
        {
            vp.Pattern.SetValue(text);
            return Task.CompletedTask;
        }

        // Last resort: focus + synthesize typing
        el.Focus();
        Keyboard.Type(text);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the element’s visible text/value using native patterns when available; falls back to the accessible name.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current text, or <c>null</c> if the control does not expose text.</returns>
    /// <remarks>
    /// Preference order:
    /// <list type="number">
    ///   <item><description><c>TextPattern.DocumentRange.GetText(int.MaxValue)</c> (trimmed)</description></item>
    ///   <item><description><c>ValuePattern.Value</c></description></item>
    ///   <item><description><c>LegacyIAccessible.Value</c> (if present)</description></item>
    ///   <item><description><c>TextBox.Text</c> for Edit controls</description></item>
    ///   <item><description><c>AutomationElement.Name</c> (e.g., Calculator results “Display is 5”)</description></item>
    /// </list>
    /// </remarks>
    public Task<string?> GetTextAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Prefer TextPattern (common on modern XAML)
        var tp = el.Patterns.Text;
        if (tp.IsSupported)
        {
            var range = tp.Pattern.DocumentRange;
            var txt = range?.GetText(int.MaxValue) ?? string.Empty;
            return Task.FromResult(txt?.TrimEnd('\r', '\n'));
        }

        // ValuePattern (common on editable controls)
        var vp = el.Patterns.Value;
        if (vp.IsSupported)
        {
            return Task.FromResult(vp.Pattern.Value.Value)!;
        }

        // Legacy IAccessible as a fallback
        var legacy = el.Patterns.LegacyIAccessible;
        if (legacy.IsSupported)
        {
            var v = legacy.Pattern.Value.Value;
            if (!string.IsNullOrEmpty(v)) return Task.FromResult(v)!;
            // Some controls use Name even when legacy exists
        }

        // If it's actually an Edit, try TextBox
        if (el.ControlType == ControlType.Edit)
        {
            var tb = el.AsTextBox();
            try
            {
                return Task.FromResult(tb.Text)!;
            }
            catch
            {
                // Ignore and fall through to Name
            }
        }

        // Final fallback: accessible name
        return Task.FromResult(el.Name)!;
    }

    /// <summary>
    /// Checks whether this handle still refers to an existing (available) element.
    /// </summary>
    /// <param name="timeout">
    /// Unused in this implementation; existence is evaluated immediately. Reserved for future implementations
    /// that may attempt re-validation within a time budget.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the element is still available; otherwise <c>false</c>.</returns>
    public Task<bool> ExistsAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(el?.IsAvailable == true);
    }

    /// <summary>
    /// Waits until the element becomes visible and enabled.
    /// </summary>
    /// <param name="timeout">
    /// Optional overall timeout. If <c>null</c>, defaults to 5 seconds in this implementation.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the element is visible/enabled.</returns>
    /// <exception cref="TimeoutException">Thrown if the element does not become visible/enabled in time.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public async Task WaitForVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var t = timeout ?? TimeSpan.FromSeconds(5);
        var deadline = DateTime.UtcNow + t;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var visible = !(el is null || (SafeProperty.TryIsOffscreen(el, out var off) && off));
            var enabled = true;
            SafeProperty.TryIsEnabled(el, out enabled);

            if (visible && enabled) return;

            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        throw new TimeoutException("Element did not become visible/enabled in time.");
    }
}