using FlaUI.Core.AutomationElements;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Safe accessors for UIA properties that may be unsupported or throw on certain providers.
/// </summary>
/// <remarks>
/// <para>
/// Some UI Automation providers (especially on packaged/Store apps or custom controls) can throw when
/// reading properties like <c>IsOffscreen</c> or <c>IsEnabled</c>, or simply report them as unavailable.
/// These helpers wrap those reads and return a boolean indicating whether the read succeeded while
/// always producing a usable <c>out</c> value.
/// </para>
/// <para>
/// <b>Return contract:</b> The methods return <c>true</c> if the property value was retrieved successfully.
/// If <c>false</c> is returned, the <c>out</c> value contains a sensible fallback (documented per method) so
/// callers can proceed without throwing or littering code with try/catch.
/// </para>
/// <para><b>Thread-safety:</b> Methods are stateless and thread-safe.</para>
/// </remarks>
internal static class SafeProperty
{
    /// <summary>
    /// Attempts to read <see cref="AutomationElement.Properties"/>.<c>IsOffscreen</c> without throwing.
    /// </summary>
    /// <param name="el">The automation element to query.</param>
    /// <param name="isOffscreen">
    /// When this method returns, contains the off-screen flag if retrieval succeeded; otherwise <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the provider returned a value for <c>IsOffscreen</c>; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If the property is unavailable or the provider throws, this method returns <c>false</c> and sets
    /// <paramref name="isOffscreen"/> to <c>false</c> (treating the element as visible by default).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="el"/> is <c>null</c>.</exception>
    public static bool TryIsOffscreen(AutomationElement el, out bool isOffscreen)
    {
        ArgumentNullException.ThrowIfNull(el);

        try
        {
            var ap = el.Properties.IsOffscreen;
            if (ap.TryGetValue(out var v))
            {
                isOffscreen = v;
                return true;
            }
        }
        catch
        {
            /* ignore provider exceptions */
        }

        isOffscreen = false; // default: consider on-screen if uncertain
        return false;
    }

    /// <summary>
    /// Attempts to read <see cref="AutomationElement.Properties"/>.<c>IsEnabled</c> without throwing.
    /// </summary>
    /// <param name="el">The automation element to query.</param>
    /// <param name="isEnabled">
    /// When this method returns, contains the enabled flag if retrieval succeeded; otherwise <c>true</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the provider returned a value for <c>IsEnabled</c>; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If the property is unavailable or the provider throws, this method returns <c>false</c> and sets
    /// <paramref name="isEnabled"/> to <c>true</c> (an optimistic default so interactions can still be attempted
    /// when the platform does not surface an enabled state).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="el"/> is <c>null</c>.</exception>
    public static bool TryIsEnabled(AutomationElement el, out bool isEnabled)
    {
        ArgumentNullException.ThrowIfNull(el);

        try
        {
            var ap = el.Properties.IsEnabled;
            if (ap.TryGetValue(out var v))
            {
                isEnabled = v;
                return true;
            }
        }
        catch
        {
            /* ignore provider exceptions */
        }

        isEnabled = true; // default: assume enabled if uncertain
        return false;
    }
}